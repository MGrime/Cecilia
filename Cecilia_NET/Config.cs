namespace Cecilia_NET
{
    public class Config
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public string SpotifyKey { get; set; }

        public Config()
        {
            Token = "";
            Prefix = "";
            SpotifyKey = "";
        }
    }
}