// Cecilia.NET
// Yet another music bot written in Discord.NET
// By Michael Grime 12/08/2020

// Console access
// TAP access
// Discord API
// Discord web connection
using System;
using System.Threading.Tasks;
using Cecilia_NET.Services;
using Cecilia.NET;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using static System.String;    // Speeds up string comparison calls

namespace Cecilia_NET
{
    public class Bot
    {
        // Transfer to an async main method after acquiring token
        public static void Main(string[] args)
        {
            // Extract token from .txt in root directory
            var token = "";
            while (Compare(token, "", StringComparison.Ordinal) == 0)
            {
                try
                {
                    token = System.IO.File.ReadAllText(@"token.txt").Replace(Environment.NewLine,"");
                }
                catch (System.IO.FileNotFoundException)
                {
                    Console.WriteLine("ERROR: token.txt not found. Create a file with a Discord Token in the directory of the DLL. Press escape to quit! Press any other key to hot reload!");
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                    {
                        return;
                    }
                }
            }

            // Transfer to async
            // Catching all exceptions that reach here just to clean up then close
            new Bot().MainASync(token).GetAwaiter().GetResult();
        }

        // Allows for async calls to Discord.NET
        private async Task MainASync(string token)
        {
            // Create base for client
            _client = new DiscordSocketClient();
            
            // Link to logging method
            _client.Log += LogAsync;
            
            // Request login!
            await _client.LoginAsync(TokenType.Bot, token);
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

            _commandHandler = new CommandHandler(_client,_commandService,_services,"--");  
            // Register commands
            await _commandHandler.InstallCommandsAsync();

            // Now login is okay and commands are registered we can start               
            await _client.StartAsync();                     
            
            // Block this main task until program is closed
            await Task.Delay(-1);
        }
        
        // Log to console for now
        // TODO: Link to a proper logging system. Perhaps even something GUI based for bot management.
        private Task LogAsync(LogMessage msg )
        {
            // Log to console
            Console.WriteLine(msg.ToString());
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

    }
}