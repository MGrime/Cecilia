using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Cecilia_NET
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
            _commandQueue = new Queue<QueuedCommand>();
            _commandExecuting = false;
            _mutex = new Mutex();
        }
        
        // Finds all of the command modules in the solution and register them to the command service
        public async Task InstallCommandsAsync()
        {
            // Hook the message received client to our command handler
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
            // I don't like long code so im willing to make this compromise with space
            var prefixed = message.HasStringPrefix(_commandPrefix, ref argPos);
            var mentioned = message.HasMentionPrefix(_client.CurrentUser, ref argPos);
            var isBot = message.Author.IsBot;
            if ((!prefixed || mentioned) || isBot)
            {
                return;
            }
            
            // Create command context based on the message
            var context = new SocketCommandContext(_client,message);
            
            // Check if command is correct prefix but invalid
            var commandExists = _commandService.Search(context, argPos);
            if (!commandExists.IsSuccess)
            {
                Helpers.DeleteUserCommand(context);
                var response = Helpers.CeciliaEmbed(context);
                response.AddField("Invalid command!", "Do --help to see all my commands!");
                await context.Channel.SendMessageAsync("",false,response.Build());
                return;
            }
            
            // Create a command
            var command = new QueuedCommand(_commandService,context,argPos,_services);
            // Add to the queue
            if (message.Content.Contains($"{Bot.BotConfig.Prefix}play"))
            {
                await Task.Delay(1000);
            }

            _commandQueue.Enqueue(command);
            if (_commandQueue.Count != 0)
            {
                _mutex.WaitOne(-1);
                if (!_commandExecuting)
                {
                    _commandExecuting = true;

                    while (_commandExecuting)
                    {
                        // Execute
                        var executingCommand = _commandQueue.Dequeue();
                        await executingCommand.Execute();
                        // Check queue. if 0 set _commandExecutuing to false
                        if (_commandQueue.Count == 0)
                        {
                            _commandExecuting = false;
                        }
                    }
                }
                _mutex.ReleaseMutex();
            }

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
        
        // Command queuing
        private Queue<QueuedCommand> _commandQueue;
        private Mutex _mutex;
        private bool _commandExecuting;


        // Command for the queue
        internal class QueuedCommand
        {
            private readonly CommandService _commandService;
            private SocketCommandContext _context;
            private int _argPos;
            private readonly IServiceProvider _services;

            public QueuedCommand(CommandService commandService,SocketCommandContext context, int argPos,IServiceProvider service)
            {
                _context = context;
                _argPos = argPos;
                _commandService = commandService;
                _services = service;
            }

            public async Task Execute()
            {
                await _commandService.ExecuteAsync(_context, _argPos, _services);
            }
        }
    }
}