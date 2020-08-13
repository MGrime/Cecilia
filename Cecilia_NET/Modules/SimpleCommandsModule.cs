using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Cecilia_NET.Modules
{
    // Collection of simple commands such as ping, uptime etc
    public class SimpleCommandsModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Pings a message back. Allows checking of health")]
        public async Task PingAsync()
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync($"Pong! (Requested by {Context.User.Username})");
        }

        [Command("whois")]
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
            embedBuilder.AddField("Profile Pic", guildUser.GetAvatarUrl());
            // Output nickname if they have one
            embedBuilder.AddField("Name", Helpers.GetDisplayName(Context.User) );

            await Context.Channel.SendMessageAsync("",false,embedBuilder.Build());

        }
    }
}