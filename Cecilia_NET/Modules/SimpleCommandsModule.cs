using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Cecilia_NET.Modules
{
    // Collection of simple commands such as ping, uptime etc
    public class SimpleCommandsModule : ModuleBase<SocketCommandContext>
    {
        public SimpleCommandsModule(CommandService commandService)
        {
            _commandService = commandService;
        }
        
        [Command("Ping")]
        [Summary("Pings a message back. Allows checking of health")]
        public async Task PingAsync()
        {
            await Context.Channel.SendMessageAsync($"Pong! (Requested by {Context.User.Username})");
            // Delete sending message
            await Context.Message.DeleteAsync();
        }

        [Command("WhoIs")]
        [Summary("Sends an embed with data on the attached user")]
        public async Task WhoisAsync([Summary("The user to query")] SocketUser user)
        {
            // Cast to guild specific data
            if (!(user is SocketGuildUser guildUser)) return;
            
            // Prepare to build embed
            var embedBuilder = new EmbedBuilder();
            
            // Set title and image
            // Title is the users username
            embedBuilder.WithTitle(user.Username);
            // Image is profile pic
            embedBuilder.WithImageUrl(guildUser.GetAvatarUrl());
            // Output nickname if they have one
            embedBuilder.AddField("Name", Helpers.GetDisplayName(Context.User) );
            // Footer with requester
            embedBuilder.WithFooter($"Requested by {Helpers.GetDisplayName(Context.User)}");

            await Context.Channel.SendMessageAsync("",false,embedBuilder.Build());
            // Delete sending message
            await Context.Message.DeleteAsync();

        }
        
        [Command("Help")]
        public async Task Help()
        {
            Helpers.DeleteUserCommand(Context);
            
            List<CommandInfo> commands = _commandService.Commands.ToList();
            EmbedBuilder embedBuilder = Helpers.CeciliaEmbed(Context);

            embedBuilder.WithTitle("Command List");
            // Remove footer for dm
            embedBuilder.WithFooter("");
            foreach (CommandInfo command in commands)
            {
                // Get the command Summary attribute information
                string embedFieldText = command.Summary ?? "No description available\n";

                embedBuilder.AddField(command.Name, embedFieldText);
            }
            
            await Context.User.SendMessageAsync("", false, embedBuilder.Build());

            EmbedBuilder newEmbed = Helpers.CeciliaEmbed(Context);

            newEmbed.AddField("Check your DM's!","I sent you all my commands!");

            await Context.Channel.SendMessageAsync("", false,  newEmbed.Build());
        }

        private readonly CommandService _commandService;
    }
}