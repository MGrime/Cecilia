using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Cecilia_NET.Services
{
    public class MusicPlayer
    {
        private Process _ffmpeg; // The process for the FFMPEG library

        private Stream _output; // The FFMPEG library ouput stream

        public MusicPlayer() // Constructor
        {
            _activeAudioClients = new Dictionary<ulong, WrappedAudioClient>();
        }

        public void CloseFileStreams() // Closes FFMPEG, releasing file locks
        {
            if (_ffmpeg != null && _output != null) // If the process and output are null
            {
                _output.Close(); // Close the output first
                _ffmpeg.Close(); // Then close the process
            }
            else
            {
                Bot.CreateLogEntry(LogSeverity.Error, "Music Player", "ERROR: \"ffmpeg\" or \"output\" is null - cannot close streams! (See MusicPlayer.cs - Line 28)");
            }
        }
        
        public void RegisterAudioClient(ulong guildId,IAudioClient client)
        {
            // Add the audio client
            _activeAudioClients.Add(guildId, new WrappedAudioClient(client));

            Bot.CreateLogEntry(LogSeverity.Info,"Music Player","Client Added");
        }

        public void RemoveAudioClient(ulong guildId)
        {
            _activeAudioClients.Remove(guildId);

            Bot.CreateLogEntry(LogSeverity.Info,"Music Player","Client Removed");
        }

        public async Task<EmbedBuilder> AddSongToQueue(SocketCommandContext context,string filePath,Video videoData,IStreamInfo streamData,bool toDownload)
        {
            var activeClient = _activeAudioClients[context.Guild.Id];
            // THIS METHOD REQUIRES A MUTEX INCASE MULTIPLE SONGS ARE QUEUED UP IN QUICK SUCCESSION
            // Find the mutex for this queue
            var mutex = _activeAudioClients[context.Guild.Id].QueueMutex;
            // Wait for it to be free
            mutex.WaitOne(-1);
            await Bot.CreateLogEntry(LogSeverity.Info,"Music Player","Adding to queue for guild: " + context.Guild.Id);
            // Add song to queue
            activeClient.Queue.AddLast(new QueueEntry(filePath,videoData,streamData,Helpers.CeciliaEmbed(context),!toDownload));
            // Release mutex
            await Bot.CreateLogEntry(LogSeverity.Info,"Music Player","Added to queue for guild: " + context.Guild.Id);
            mutex.ReleaseMutex();
            
            // Check if we need to download
            // If the queue is empty or queue size is one then there is nothing playing so download, or it is next up so download
            // If queue is > 1 in size then there is something playing, and there is a song ready, so dont download. mark not downloaded
            // However if this is false then it is already downloaded so just ignore
            if (toDownload)
            {
                // Check queue
                if (activeClient.Queue.Count <= 2)
                {
                    await Helpers.DownloadSong(activeClient.Queue.Last.Value.StreamInfo, activeClient.Queue.Last.Value.FilePath);
                    activeClient.Queue.Last.Value.IsDownloaded = true;
                }
                else
                {
                    // Mark download needed
                    activeClient.Queue.Last.Value.IsDownloaded = false;
                }
            }
            
            // create embed
            // Caching so it can be modified for playing message
            var activeEmbed = _activeAudioClients[context.Guild.Id].Queue.Last.Value.EmbedBuilder;
            activeEmbed.WithImageUrl(videoData.Thumbnails.MediumResUrl);
            activeEmbed.WithTitle("Added song!");// This can be switched later
            activeEmbed.AddField("Title",$"[{videoData.Title}]({videoData.Url})");
            activeEmbed.AddField("Length", videoData.Duration.Minutes + " min " + videoData.Duration.Seconds + " secs");
            activeEmbed.AddField("Uploader", videoData.Author);
            activeEmbed.AddField("Queue Position", _activeAudioClients[context.Guild.Id].Queue.Count);

            // Pass back
            return activeEmbed;
        }

        public async Task PlayAudio(SocketCommandContext context)
        {
            // TODO: add checks if this used outside of the add song command
            // Find correct client
            var activeClient = _activeAudioClients[context.Guild.Id];
            if (activeClient != null)
            {
                var previousSkipStatus = false;
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
                        // Check if song is downloaded
                        if (!activeClient.Queue.First.Value.IsDownloaded)
                        {
                            await Helpers.DownloadSong(activeClient.Queue.First.Value.StreamInfo, activeClient.Queue.First.Value.FilePath);
                        }
                        // Get song from queue
                        var filePath = activeClient.Queue.First.Value.FilePath;
                        _ffmpeg = CreateStream(filePath);
                        // Setup ffmpeg output
                        _output = _ffmpeg.StandardOutput.BaseStream;
                        // Create discord pcm stream
                        await using var discord = activeClient.Client.CreatePCMStream(AudioApplication.Music);
                        // Set speaking indicator
                        await activeClient.Client.SetSpeakingAsync(true);
                        // Send playing message
                        // Modify embed
                        var activeEmbed = activeClient.Queue.First.Value.EmbedBuilder;
                        // Set playing title
                        activeEmbed.WithTitle("Now Playing!");
                        // Remove queue counter at the end of fields
                        activeEmbed.Fields.RemoveAt(activeEmbed.Fields.Count - 1);
                        //
                        var spotifyQuery = Helpers.SpotifyQuery(activeClient.Queue.First.Value.MetaData.Title);

                        if (spotifyQuery != null)
                        {
                            var spotifyHyperlink = $" [Listen on Spotify](https://open.spotify.com/track/{spotifyQuery.Result[0].Id})";

                            activeEmbed.AddField("Music Platforms",spotifyHyperlink);
                        }

                        // Send
                        var message = await context.Channel.SendMessageAsync("", false, activeEmbed.Build());
                        // Stream and await till finish
                        while (true)
                        {
                            // Stream is over, broken, or skip requested
                            if (_ffmpeg.HasExited || discord == null || activeClient.Skip)
                            {
                                previousSkipStatus = activeClient.Skip;
                                CloseFileStreams();
                                break;
                            }
                            
                            // Pause function while not playing
                            if (activeClient.Paused) continue;
                            
                            // Read a block of stream
                            int blockSize = 1920;
                            byte[] buffer = new byte[blockSize];
                            var byteCount = await _output.ReadAsync(buffer, 0, blockSize);
                            
                            // Stream cannot be read or file is ended
                            if (byteCount <= 0) break;

                            // Write output to stream
                            try
                            {
                                await discord.WriteAsync(buffer, 0, byteCount);
                            }
                            catch (Exception e)
                            {
                                // Flush buffer
                                discord?.FlushAsync().Wait();
                                // Output exception
                                await Bot.CreateLogEntry(LogSeverity.Error, "MusicPlayer", e.ToString());
                                // Delete now-playing as it is now out of date
                                await context.Channel.DeleteMessageAsync(message.Id);
                                throw;
                            }
                        }

                        // Delete now-playing as it is now out of date
                        await context.Channel.DeleteMessageAsync(message.Id);

                        // Flush buffer
                        discord?.FlushAsync().Wait();

                        // Delete used file && release queue
                        activeClient.Queue.RemoveFirst();
                        // Check queue. If same song is queued do not delete file
                        // TODO: THIS MIGHT BREAK IF MULTIPLE SERVERS QUEUE THE SAME SONG AT THE SAME TIME SO COME LOOK AT THIS WHEN YOU CAN BE BOTHERED
                        var found = false;
                        // Check all active queues
                        foreach (var client in ActiveAudioClients)
                        {
                            if (client.Value.Queue.Count != 0)
                            {
                                // Check file before cont
                                // Stops file deleting if same song is multi queued
                                // TODO: This sucks. make it more efficient - Michael
                                // Check each item in this queue
                                foreach (var queueItem in activeClient.Queue)
                                {
                                    // If we find the song in queue
                                    if (queueItem.FilePath.Equals(filePath))
                                    {
                                        // Mark for no delete
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            // We have found it, dont look through other queues
                            if (found)
                            {
                                break;
                            }
                        }
                        // If this is still false all queues have been check and it isnt there
                        if (!found)
                        {
                            // Catch windows requirement for File.Delete
                            try
                            {
                                if (Bot.OsPlatform == OSPlatform.Windows)
                                {
                                    string fullPath = Directory.GetCurrentDirectory() + @"\" + filePath;

                                    System.IO.File.Delete(fullPath);
                                }
                                else
                                {
                                    System.IO.File.Delete(filePath);
                                }
                                
                            }
                            catch (Exception e)
                            {
                                await Bot.CreateLogEntry(LogSeverity.Error,"Music Player",e.ToString());
                            }
                        }


                        // No more songs so exit
                        if (activeClient.Queue.Count == 0)
                        {
                            activeClient.Playing = false;
                        }
                        // Reset skip trigger
                        activeClient.Skip = false;
                        await activeClient.Client.SetSpeakingAsync(false);
                    }
                }

                if (!previousSkipStatus)
                {
                    var response = Helpers.CeciliaEmbed(context);
                    response.AddField("That's all folks!", "Spin up some more songs with the play command!");
                    await context.Channel.SendMessageAsync("", false, response.Build());
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
            private LinkedList<QueueEntry> _queue;
            private Mutex _mutex;
            
            // Control over playing
            private bool _playing;
            private bool _paused;
            private bool _skip;

            public WrappedAudioClient(IAudioClient client)
            {
                _client = client;
                _queue = new LinkedList<QueueEntry>();
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

            public LinkedList<QueueEntry> Queue
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

        public class QueueEntry
        {
            public string FilePath { get; set; }
            public Video MetaData { get; set; }
            public IStreamInfo StreamInfo { get; set; }
            public EmbedBuilder EmbedBuilder { get; set; }
            
            public bool IsDownloaded { get; set; }

            public QueueEntry(string filePath, Video metaData, IStreamInfo streamInfo, EmbedBuilder embedBuilder, bool isDownloaded)
            {
                FilePath = filePath;
                MetaData = metaData;
                StreamInfo = streamInfo;
                EmbedBuilder = embedBuilder;
                IsDownloaded = isDownloaded;
            }
        }
        private readonly Dictionary<ulong,WrappedAudioClient> _activeAudioClients;

        public Dictionary<ulong, WrappedAudioClient> ActiveAudioClients => _activeAudioClients;
    }
}