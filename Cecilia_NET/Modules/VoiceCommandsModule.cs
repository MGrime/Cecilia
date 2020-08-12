using System.Threading.Tasks;
using Cecilia_NET.Services;
using Discord;
using Discord.Commands;

namespace Cecilia_NET.Modules
{
    public class VoiceCommandsModule : ModuleBase<SocketCommandContext>
    {
        public VoiceCommandsModule(MusicPlayer player)
        {
            _musicPlayer = player;
        }
        
        [Command("join",RunMode = RunMode.Async)]
        [Summary("Joins the voice channel of the user. Alternatively pass in a channel with a ref")]
        public async Task JoinAsync(IVoiceChannel channel = null)
        {
            // Check for pre-existing connection
            if (Context.Guild.AudioClient != null)
            {
                await Context.Channel.SendMessageAsync(
                    "I'm already connected! Drag me to a different room if you want to switch.");
                return;
            }
            
            // If none then check channel is valid
            // This uses compound assignment:
            // If it is not null, nothing happens
            // If it IS null, it is set to the value of the assignment.
            // In this case that checks if the user is connected into a channel
            channel ??= (Context.User as IGuildUser)?.VoiceChannel;
            
            // Here null means no channel specified and no user is not in a channel
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Please first join a channel! Alternatively specify a valid channel as an argument");
                return;
            }
            
            // Now we have a valid context, channel and guild
            var client = await channel.ConnectAsync(true);
            
            _musicPlayer.RegisterAudioClient(Context.Guild.Id,client);

            await Context.Channel.SendMessageAsync("I've connected!");
        }

        [Command("leave",RunMode = RunMode.Async)]
        [Summary("Leaves the voice channel if we are connected to one")]
        public async Task LeaveAsync()
        {
            // Check for connection
            if (Context.Guild.AudioClient == null)
            {
                return;
            }
            // There is a connect in this guild
            // Disconnect
            await Context.Guild.AudioClient.StopAsync();
            
            _musicPlayer.RemoveAudioClient(Context.Guild.Id);
            
            // Now we have disconnected
            await Context.Channel.SendMessageAsync("I've disconnected!");

        }

        private readonly MusicPlayer _musicPlayer;
    }
}