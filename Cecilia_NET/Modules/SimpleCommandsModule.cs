using System.Threading.Tasks;
using Discord.Commands;

namespace Cecilia_NET.Modules
{
    // Collection of simple commands such as ping, uptime etc
    public class SimpleCommandsModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Pings a message back. Allows checking of health")]
        public Task PingAsync() => ReplyAsync("Pong!");
    }
}