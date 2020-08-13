using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
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

        public void AddSongToQueue(SocketCommandContext context,string filePath,Video videoData, ref EmbedBuilder addedEmbed)
        {
            // THIS METHOD REQUIRES A MUTEX INCASE MULTIPLE SONGS ARE QUEUED UP IN QUICK SUCCESSION
            // Find the mutex for this queue
            var mutex = _activeAudioClients[context.Guild.Id].QueueMutex;
            // Just make sure
            if (mutex == null)
            {
                return;
            }
            // Wait for it to be free
            mutex.WaitOne(-1);
            Console.WriteLine("Adding to queue for guild: " + context.Guild.Id);
            // Add song to queue
            _activeAudioClients[context.Guild.Id].Queue.AddLast(new Tuple<string, EmbedBuilder>(filePath,new EmbedBuilder()));
            // Release mutex
            Console.WriteLine("Added to queue for guild: " + context.Guild.Id);
            mutex.ReleaseMutex();
            
            // create embed
            // Caching so it can be modified for playing message
            var activeEmbed = _activeAudioClients[context.Guild.Id].Queue.Last.Value.Item2;
            activeEmbed.WithAuthor(context.Client.CurrentUser.Username, context.Client.CurrentUser.GetAvatarUrl());
            activeEmbed.WithImageUrl(videoData.Thumbnails.MediumResUrl);
            activeEmbed.WithTitle("Added song!");// This can be switched later
            activeEmbed.AddField("Title", videoData.Title);
            activeEmbed.AddField("Length", videoData.Duration.Minutes + " min " + videoData.Duration.Seconds + " secs");
            activeEmbed.AddField("Uploader", videoData.Author);
            activeEmbed.AddField("Queue Position", _activeAudioClients[context.Guild.Id].Queue.Count);
            activeEmbed.WithFooter($"Requested by {context.User.Username}");

            // Pass back
            addedEmbed = activeEmbed;
        }

        public async Task PlayAudio(SocketCommandContext context)
        {
            // TODO: add checks if this used outside of the add song command
            // Find correct client
            var activeClient = _activeAudioClients[context.Guild.Id];
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
                        await using var discord = activeClient.Client.CreatePCMStream(AudioApplication.Music);
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
                        var message = await context.Channel.SendMessageAsync("", false, activeEmbed.Build());
                        // Stream and await till finish
                        while (true)
                        {
                            // Stream is over, broken, or skip requested
                            if (ffmpeg.HasExited || discord == null || activeClient.Skip) break;
                            
                            // Pause function while not playing
                            if (activeClient.Paused) continue;
                            
                            // Read a block of stream
                            int blockSize = 1920;
                            byte[] buffer = new byte[blockSize];
                            var byteCount = await ffmpeg.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize);
                            
                            // Stream cannot be read or file is ended
                            if (byteCount <= 0) break;
                            
                            // Write output to stream
                            try
                            {
                                await discord.WriteAsync(buffer, 0, byteCount);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        }

                        // Delete now-playing as it is now out of date
                        await context.Channel.DeleteMessageAsync(message.Id);

                        // Flush buffer
                        discord?.FlushAsync().Wait();
                        
                        // Reset skip trigger
                        activeClient.Skip = false;

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
            // The raw client
            private IAudioClient _client;
            
            // Queue and a mutex for accessing
            private LinkedList<Tuple<string,EmbedBuilder>> _queue;
            private Mutex _mutex;
            
            // Control over playing
            private bool _playing;
            private bool _paused;
            private bool _skip;

            public WrappedAudioClient(IAudioClient client)
            {
                _client = client;
                _queue = new LinkedList<Tuple<string,EmbedBuilder>>();
                _playing = false;
                _paused = false;
                _skip = false;
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

            public bool Paused
            {
                get => _paused;
                set => _paused = value;
            }

            public bool Skip
            {
                get => _skip;
                set => _skip = value;
            }

            public Mutex QueueMutex
            {
                get => _mutex;
                set => _mutex = value;
            }

        }
        private readonly Dictionary<ulong,WrappedAudioClient> _activeAudioClients;

        public Dictionary<ulong, WrappedAudioClient> ActiveAudioClients => _activeAudioClients;
    }
}