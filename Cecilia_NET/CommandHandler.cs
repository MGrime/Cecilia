using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Cecilia.NET
{
    public class CommandHandler
    {
        // Constructor
        public CommandHandler(DiscordSocketClient client, CommandService commandService,IServiceProvider services, string desiredPrefix = "$")
        {
            _commandService = commandService;
            _client = client;
            _services = services;
            _commandPrefix = desiredPrefix;
        }
        
        // Finds all of the command modules in the solution and register them to the command service
        public async Task InstallCommandsAsync()
        {
            // Hook the message recieved client to our command handler
            _client.MessageReceived += HandleCommandAsync;
            
            // Load all modules within assembly to register commands
            // IMPORTANT NOTE ABOUT MODULES
            // LIFETIME OF MODULE IS ONLY AS COMMAND IS INVOKED. PERSISTENT DATA MUST BE STORED IN A DIFFERENT OBJECT
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(),_services);
        }
        
        // Pre-process an incoming message to find correct command
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Ignore system messages
            if (!(messageParam is SocketUserMessage message)) return;
            
            // Track command pos after prefix
            var argPos = 0;
            
            // Make sure message is a command with correct prefix
            // Also ignore all bots command
            // I don't like long code so im willing to make this comprimise with space
            var prefixed = message.HasStringPrefix(_commandPrefix, ref argPos);
            var mentioned = message.HasMentionPrefix(_client.CurrentUser, ref argPos);
            var isBot = message.Author.IsBot;
            if ((!prefixed || mentioned) || isBot)
            {
                return;
            }
            
            // Create command context based on the message
            var context = new SocketCommandContext(_client,message);
            
            // Execute the command with the context and check preconditions with service provide
            await _commandService.ExecuteAsync(context, argPos, _services);
        }

        // Member variables
        // Passed in from Cecilia.NET.Bot
        // Readonly modifier as the commandhandler doesnt need to change them, merely send instructions and get connections
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        
        // From config
        // TODO: Add a config file if this gets big
        private readonly string _commandPrefix;
    }
}