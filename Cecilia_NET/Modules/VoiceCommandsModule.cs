using System.Threading.Tasks;
using Cecilia_NET.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;


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

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Adds a song to the play queue")]
        public async Task PlayAsync([Remainder] [Summary("The URL to play.")] string uri)
        {
            // Check if connected to voice
            if (Context.Guild.AudioClient == null)
            {
                // We aren't connected. But if the user is in a channel auto connect
                var user = Context.User as SocketGuildUser;
                if (user?.VoiceChannel != null)
                {
                    await JoinAsync(user.VoiceChannel);
                }
                // Else we fail out
                else
                {
                    await Context.Channel.SendMessageAsync(
                        "I'm not in a voice channel and neither are you! Please join one and re-run the command.");
                }
                // Then continue if connected
            }
            
            // TODO: TAKE LINK. DOWNLOAD YOUTUBE AS MP3. ADD TO QUEUE
            // Start youtube explode
            var youtube = new YoutubeClient();
            // Get video metadata
            var video = await youtube.Videos.GetAsync(uri);
            // get streams
            var streams = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            // Pick best audio stream
            var streamInfo = streams.GetAudioOnly().WithHighestBitrate();
            // Download
            if (streamInfo != null)
            {
                // Download
                await youtube.Videos.Streams.DownloadAsync(streamInfo, $"AudioCache/{video.Title}.mp3");
            }
            
            // 3. Add to queue
            _musicPlayer.AddSongToQueue(Context.Guild.Id,$"AudioCache/{video.Title}.mp3");

            // 4. Notify added
            await Context.Channel.SendMessageAsync($"Added {uri} to the queue!");
        }

        private readonly MusicPlayer _musicPlayer;
    }
}