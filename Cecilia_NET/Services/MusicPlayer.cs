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
            _activeAudioClients = new List<Tuple<ulong, IAudioClient, List<string>>>();
            _queueMutexes = new List<Tuple<ulong, Mutex>>();
        }
        
        public void RegisterAudioClient(ulong guildId,IAudioClient client)
        {
            // Add the audio client
            _activeAudioClients.Add(new Tuple<ulong, IAudioClient, List<string>>(guildId,client,new List<string>()));
            _queueMutexes.Add(new Tuple <ulong,Mutex>(guildId,new Mutex()));
            
            Console.WriteLine("Client added!");
        }

        public void RemoveAudioClient(ulong guildId)
        {
            var client =_activeAudioClients.FindIndex(x => x.Item1 == guildId);
            _activeAudioClients.RemoveAt(client);
            _queueMutexes.RemoveAt(client);    // This works as they are always in sync
            
            Console.WriteLine("Client removed!");
        }

        public void AddSongToQueue(ulong guildId,string filePath)
        {
            // THIS METHOD REQUIRES A MUTEX INCASE MULTIPLE SONGS ARE QUEUED UP IN QUICK SUCCESSION
            // Find the mutex for this queue
            var mutex = _queueMutexes.Find(x => x.Item1 == guildId)?.Item2;// There is always a mutex for each queue
            // Just make sure
            if (mutex == null)
            {
                return;
            }
            // Wait for it to be free
            mutex.WaitOne(-1);
            Console.WriteLine("Adding to queue for guild: " + guildId);
            // Add song to queue
            _activeAudioClients.Find(x=> x.Item1 == guildId)?.Item3.Add(filePath);
            // Release mutex
            Console.WriteLine("Added to queue for guild: " + guildId);
            mutex.ReleaseMutex();
            
        }
        
        // List of active audio clients
        private readonly List<Tuple<ulong,Mutex>> _queueMutexes;
        private readonly List<Tuple<ulong,IAudioClient,List<string>>> _activeAudioClients;
    }
}