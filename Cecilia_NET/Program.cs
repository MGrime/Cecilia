// Cecilia.NET
// Yet another music bot written in Discord.NET
// By Michael Grime 12/08/2020

// Console access
// TAP access
// Discord API
// Discord web connection
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cecilia_NET.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using YouTubeSearch;
using static System.String;    // Speeds up string comparison calls

namespace Cecilia_NET
{
    public class Bot
    {
        // Transfer to an async main method after acquiring token
        public static void Main(string[] args)
        {
            // Find out platform
            OsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows : OSPlatform.Linux;
            
            // Create the configs
            BotConfig = new DiscordConfig();
            SpotifyConfig = null;

            // Check for a create files if needed
            if (!File.Exists(@"bot.json"))
            {
                File.Create(@"bot.json").Close();
            }

            if (!File.Exists(@"spotify.json"))
            {
                File.Create(@"spotify.json").Close();
            }
            

            while (BotConfig.Token == "" || BotConfig.Prefix == "")
            {
                // Read in the config
                var rawConfig = System.IO.File.ReadAllText(@"bot.json").Replace(Environment.NewLine,"");
                BotConfig = JsonConvert.DeserializeObject<DiscordConfig>(rawConfig) ?? new DiscordConfig();
                // This means ^ failed so create the memory for the configs
                if (BotConfig.Token != "" && BotConfig.Prefix != "") 
                {
                    break;
                }
                // Ask for token
                while (BotConfig.Token == "")
                {
                    CreateLogEntry(LogSeverity.Info, "Setup", "Enter a Discord Bot Token (Instructions on Github): ",false).GetAwaiter().GetResult();
                    BotConfig.Token = Console.ReadLine();
                }
                // Ask for prefix
                while (BotConfig.Prefix == "")
                {
                    CreateLogEntry(LogSeverity.Info,"Setup","Enter a Command Prefix (For example -- or !): ",false).GetAwaiter().GetResult();
                    BotConfig.Prefix = Console.ReadLine();
                }
                // Save the config
                var outputConfig = JsonConvert.SerializeObject(BotConfig);
                File.WriteAllText(@"bot.json",outputConfig);
                
            }
            
            // Load in data
            var spotifyConfigRaw = File.ReadAllText(@"spotify.json".Replace(Environment.NewLine, ""));
            var spotifyClientData = JsonConvert.DeserializeObject<SpotifyClientData>(spotifyConfigRaw) ?? new SpotifyClientData();
            if (spotifyClientData.ClientId != "" && spotifyClientData.ClientSecret != "")
            {
                if (!(spotifyClientData.ClientId == "-1" || spotifyClientData.ClientId == "-1"))
                {
                    // Load into credential thing
                    SpotifyConfig = SpotifyClientConfig
                        .CreateDefault()
                        .WithAuthenticator(new ClientCredentialsAuthenticator(spotifyClientData.ClientId,spotifyClientData.ClientSecret));
                    // Notify success
                    CreateLogEntry(LogSeverity.Info, "Spotify",
                        "Loaded Spotify Integration!").GetAwaiter().GetResult();
                }

            }
            else
            {
                    // Prepare to get input
                string input = "";
                // Require them to input something
                while (input == "")
                {
                    // Ask for spotify integration
                    CreateLogEntry(LogSeverity.Info,"Spotify","Would you like to setup spotify integration? Y/N: ",false).GetAwaiter().GetResult();
                    input = Console.ReadLine();
                    // They didnt type anything so loop
                    // Lower case the input just in case
                    input = input?.ToLower();
                    // Want spotify
                    if (input == "y" || input == "yes")
                    {
                        var spotifyData = new SpotifyClientData();
                        // Get spotify input 
                        while (spotifyData.ClientId == "")
                        {
                            CreateLogEntry(LogSeverity.Info,"Spotify","Enter a client ID: ",false).GetAwaiter().GetResult();
                            spotifyData.ClientId = Console.ReadLine();
                        }
                        while (spotifyData.ClientSecret == "")
                        {
                            CreateLogEntry(LogSeverity.Info,"Spotify","Enter the client secret: ", false).GetAwaiter().GetResult();
                            spotifyData.ClientSecret = Console.ReadLine();
                        }
                        // Save config
                        var outputSpotify = JsonConvert.SerializeObject(spotifyData);
                        File.WriteAllText(@"spotify.json",outputSpotify);
                        
                        // Create spotify config
                        // Load into credential thing
                        SpotifyConfig = SpotifyClientConfig
                            .CreateDefault()
                            .WithAuthenticator(new ClientCredentialsAuthenticator(spotifyData.ClientId,spotifyData.ClientSecret));
                        // Notify success
                        Bot.CreateLogEntry(LogSeverity.Info, "Spotify",
                            "Loaded Spotify Integration!"); 
                    }
                    // Dont
                    else if (input == "n" || input == "no")
                    {
                        SpotifyConfig = null;
                        break;
                    }
                    // Didnt read the instructions, loop again
                    else
                    {
                        input = "";
                    }
                }
            }

                // Check to see if the audio cache exists
            if (!Directory.Exists("AudioCache")) // If not create it
            {
                System.IO.Directory.CreateDirectory("AudioCache");
            }
            else // If it does, clear it
            {
                System.IO.DirectoryInfo di = new DirectoryInfo("AudioCache");

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }

            // Transfer to async
            new Bot().MainASync(BotConfig).GetAwaiter().GetResult();
        }

        // Allows for async calls to Discord.NET
        private async Task MainASync(DiscordConfig config)
        {
            // Create base for client
            _client = new DiscordSocketClient();
            
            // Link to logging method
            _client.Log += LogAsyncLine;
            
            // Request login!
            await _client.LoginAsync(TokenType.Bot, config.Token);
            // UNTIL THIS LINE ^ COMPLETES CLIENT IS NOT IN A USABLE STATE
      
            // Create command service and handler                                
            _commandService = new CommandService(new CommandServiceConfig        
            {                                                                    
                CaseSensitiveCommands = false,                                   
                DefaultRunMode = RunMode.Async,                                  
                IgnoreExtraArgs = false,                                         
                LogLevel = LogSeverity.Info,    // TODO monitor this,            
                SeparatorChar = ' ',                                             
                ThrowOnError = false                                             
            });                                                   
            // Create service injector for Dependencies
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton<MusicPlayer>()    // For music audio playback
                .BuildServiceProvider();

            _commandHandler = new CommandHandler(_client,_commandService,_services,config.Prefix);  
            // Register commands
            await _commandHandler.InstallCommandsAsync();

            // Now login is okay and commands are registered we can start               
            await _client.StartAsync();

            await _client.SetGameAsync("some tunes!", null, ActivityType.Listening);
            
            
            // Block this main task until program is closed
            await Task.Delay(-1);
        }
        
        // Log to console for now
        // TODO: Link to a proper logging system. Perhaps even something GUI based for bot management.
        public static Task CreateLogEntry(LogSeverity severity,string source,string msg, bool writeLine = true)
        {
            var logMsg = new LogMessage(severity,source,msg,null);
            if (writeLine)
            {
                LogAsyncLine(logMsg);
            }
            else
            {
                LogAsync(logMsg);
            }
            return Task.CompletedTask;
        }
        public static Task LogAsyncLine(LogMessage msg)
        {
            // Log to console
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static Task LogAsync(LogMessage msg)
        {
            // Log to console
            Console.Write(msg.ToString());
            return Task.CompletedTask;
        }
        // Data members

        // The main connection to Discord
        private DiscordSocketClient _client;
        // Collection of commands from different modules loaded by handler
        private CommandService _commandService;
        // Manages the loading of modules and connection to _client
        private CommandHandler _commandHandler;
        // Manages dependency injection
        private IServiceProvider _services;
        // Config
        public static DiscordConfig BotConfig { get; set; }
        public static SpotifyClientConfig SpotifyConfig { get; set; }
        public static OSPlatform OsPlatform { get; set; }
        

    }
}