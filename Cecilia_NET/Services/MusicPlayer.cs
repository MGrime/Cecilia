using System;
using System.Collections.Generic;
using Discord.Audio;

namespace Cecilia_NET.Services
{
    public class MusicPlayer
    {
        public MusicPlayer()
        {
            _activeAudioClients = new List<Tuple<ulong, IAudioClient>>();
        }
        
        public void RegisterAudioClient(ulong guildId,IAudioClient client)
        {
            // Add the audio client
            _activeAudioClients.Add(new Tuple<ulong,IAudioClient>(guildId,client));
            
            Console.WriteLine("Client added!");
        }

        public void RemoveAudioClient(ulong guildId)
        {
            var client =_activeAudioClients.FindIndex(x => x.Item1 == guildId);
            _activeAudioClients.RemoveAt(client);
            
            Console.WriteLine("Client removed!");
        }
        
        // List of active audio clients
        private List<Tuple<ulong,IAudioClient>> _activeAudioClients;
    }
}