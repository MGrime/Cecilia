using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using YoutubeExplode.Videos;

namespace Cecilia_NET.Services
{
    public class MusicPlayer
    {
        public MusicPlayer()
        {
            _activeAudioClients = new Dictionary<ulong, WrappedAudioClient>();
        }
        
        public void RegisterAudioClient(ulong guildId,IAudioClient client)
        {
            // Add the audio client
            _activeAudioClients.Add(guildId, new WrappedAudioClient(client));

            Console.WriteLine("Client added!");
        }

        public void RemoveAudioClient(ulong guildId)
        {
            _activeAudioClients.Remove(guildId);

            Console.WriteLine("Client removed!");
        }

        public void AddSongToQueue(ulong guildId,string filePath,Video videoData, ref EmbedBuilder addedEmbed)
        {
            // THIS METHOD REQUIRES A MUTEX INCASE MULTIPLE SONGS ARE QUEUED UP IN QUICK SUCCESSION
            // Find the mutex for this queue
            var mutex = _activeAudioClients[guildId].Mutex;
            // Just make sure
            if (mutex == null)
            {
                return;
            }
            // Wait for it to be free
            mutex.WaitOne(-1);
            Console.WriteLine("Adding to queue for guild: " + guildId);
            // Add song to queue
            _activeAudioClients[guildId].Queue.AddLast(new Tuple<string, EmbedBuilder>(filePath,new EmbedBuilder()));
            // Release mutex
            Console.WriteLine("Added to queue for guild: " + guildId);
            mutex.ReleaseMutex();
            
            // create embed
            // Caching so it can be modified for playing message
            var activeEmbed = _activeAudioClients[guildId].Queue.Last.Value.Item2;
            activeEmbed.WithImageUrl(videoData.Thumbnails.MediumResUrl);
            activeEmbed.WithTitle("Added song!");// This can be switched later
            activeEmbed.AddField("Title", videoData.Title);
            activeEmbed.AddField("Length", videoData.Duration.Minutes + " min " + videoData.Duration.Seconds + " secs");
            activeEmbed.AddField("Uploader", videoData.Author);
            activeEmbed.AddField("Queue Position", _activeAudioClients[guildId].Queue.Count);

            // Pass back
            addedEmbed = activeEmbed;
        }

        public async Task PlayAudio(ulong guildId, ISocketMessageChannel channel)
        {
            // TODO: add checks if this used outside of the add song command
            // Find correct client
            var activeClient = _activeAudioClients[guildId];
            if (activeClient != null)
            {
                // Check if already playing audio
                if (activeClient.Playing)
                {
                    // exit no need
                    return;
                }
                // Check queue status
                if (activeClient.Queue.Count != 0)
                {
                    // Set playing
                    activeClient.Playing = true;
                    // While there are songs to play
                    while (activeClient.Playing)
                    {
                        // Get song from queue
                        var filePath = activeClient.Queue.First.Value.Item1;
                        using var ffmpeg = CreateStream(filePath);
                        // Setup ffmpeg output
                        await using var output = ffmpeg.StandardOutput.BaseStream;
                        // Create discord pcm stream
                        await using var discord = activeClient.Client.CreatePCMStream(AudioApplication.Mixed);

                        // Set speaking indicator
                        await activeClient.Client.SetSpeakingAsync(true);
                        
                        // Send playing message
                        // Modify embed
                        var activeEmbed = activeClient.Queue.First.Value.Item2;
                        // Set playing title
                        activeEmbed.WithTitle("Now Playing!");
                        // Remove queue counter at the end of fields
                        activeEmbed.Fields.RemoveAt(activeEmbed.Fields.Count - 1);
                        // Send
                        await channel.SendMessageAsync("", false, activeEmbed.Build());
                        // Stream and await till finish
                        try
                        {
                            await output.CopyToAsync(discord);
                        }
                        finally
                        {
                            await discord.FlushAsync();
                        }

                        // Delete used file && release queue
                        activeClient.Queue.RemoveFirst();
                        // Check queue. If same song is queued do not delete file
                        if (activeClient.Queue.Count != 0)
                        {
                            // Check file before cont
                            // Stops file deleting if same song is multi queued
                            // TODO: This sucks. make it more efficient - Michael
                            var found = false;
                            foreach (var queueItem in activeClient.Queue)
                            {
                                if (queueItem.Item1.Equals(filePath))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                System.IO.File.Delete(filePath);
                            }
                            
                            continue;
                        }
                        
                        // No more songs so exit
                        activeClient.Playing = false;
                        await activeClient.Client.SetSpeakingAsync(false);
                    }
                }
            }
            
            
        }
        
        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        
        // Wraps the client with a queue and a mutex control for data access.
        public class WrappedAudioClient
        {
            private IAudioClient _client;
            private LinkedList<Tuple<string,EmbedBuilder>> _queue;
            private bool _playing;
            private Mutex _mutex;

            public WrappedAudioClient(IAudioClient client)
            {
                _client = client;
                _queue = new LinkedList<Tuple<string,EmbedBuilder>>();
                _playing = false;
                _mutex = new Mutex();
            }

            public IAudioClient Client
            {
                get => _client;
                set => _client = value;
            }

            public LinkedList<Tuple<string,EmbedBuilder>> Queue
            {
                get => _queue;
                set => _queue = value;
            }

            public bool Playing
            {
                get => _playing;
                set => _playing = value;
            }

            public Mutex Mutex
            {
                get => _mutex;
                set => _mutex = value;
            }
        }
        private readonly Dictionary<ulong,WrappedAudioClient> _activeAudioClients;

        public Dictionary<ulong, WrappedAudioClient> ActiveAudioClients => _activeAudioClients;
    }
}