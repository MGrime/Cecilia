using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;

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

        public void AddSongToQueue(ulong guildId,string filePath)
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
            _activeAudioClients[guildId].Queue.Enqueue(filePath);
            // Release mutex
            Console.WriteLine("Added to queue for guild: " + guildId);
            mutex.ReleaseMutex();
            
        }

        public async Task PlayAudio(ulong guildId)
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
                        string filePath = activeClient.Queue.Dequeue();
                        using var ffmpeg = CreateStream(filePath);
                        // Setup ffmpeg output
                        await using var output = ffmpeg.StandardOutput.BaseStream;
                        // Create discord pcm stream
                        await using var discord = activeClient.Client.CreatePCMStream(AudioApplication.Mixed);

                        // Set speaking indicator
                        await activeClient.Client.SetSpeakingAsync(true);
                        // Stream and await till finish
                        try
                        {
                            await output.CopyToAsync(discord);
                        }
                        finally
                        {
                            await discord.FlushAsync();
                        }

                        // Delete used file
                        System.IO.File.Delete(filePath);
                        
                        // Now check queue and reloop if songs playing
                        if (activeClient.Queue.Count != 0) continue;
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
        private class WrappedAudioClient
        {
            private IAudioClient _client;
            private Queue<string> _queue;
            private bool _playing;
            private Mutex _mutex;

            public WrappedAudioClient(IAudioClient client)
            {
                _client = client;
                _queue = new Queue<string>();
                _playing = false;
                _mutex = new Mutex();
            }

            public IAudioClient Client
            {
                get => _client;
                set => _client = value;
            }

            public Queue<string> Queue
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
    }
}