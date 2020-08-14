using SpotifyAPI.Web;

namespace Cecilia_NET
{
    public class DiscordConfig
    {
        public string Token { get; set; }
        public string Prefix { get; set; }

        public DiscordConfig()
        {
            Token = "";
            Prefix = "";
        }
    }

    public class SpotifyClientData
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public SpotifyClientData()
        {
            ClientId = "";
            ClientSecret = "";
        }
    }
    
    
}