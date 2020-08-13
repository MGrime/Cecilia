using System;
using System.IO;
using System.Net;
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
            if (Context.Guild.AudioClient != null && Context.Guild.AudioClient.ConnectionState != ConnectionState.Disconnected)
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
            if (Context.Guild.AudioClient == null || Context.Guild.AudioClient.ConnectionState != ConnectionState.Connected)
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
            

            // Start youtube explode
            var youtube = new YoutubeClient();
            // Get video metadata & download thumbnail
            var video = await youtube.Videos.GetAsync(uri);
            // Check if file exists. If it does skip the download#
            // TODO: This sucks. Make it more efficient - Michael
            bool fileExists = false;
            foreach (var file in Directory.GetFiles("AudioCache/"))
            {
                if (file == $"{video.Title}.mp3")
                {
                    fileExists = true;
                    break;
                }
            }
            // Only download if it doesnt already exist
            if (!fileExists)
            {
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
            }

            // 3. Add to queue
            EmbedBuilder builder = new EmbedBuilder();
            _musicPlayer.AddSongToQueue(Context.Guild.Id,$"AudioCache/{video.Title}.mp3",video, ref builder);

            // 4. Notify added
            await Context.Channel.SendMessageAsync("", false, builder.Build());
            
            // Now play
            await _musicPlayer.PlayAudio(Context.Guild.Id,Context.Channel);
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Summary("Skips current song")]
        public async Task SkipAsync()
        {
            Console.WriteLine("Skip requested!");
            _musicPlayer.ActiveAudioClients[Context.Guild.Id].Skip = true;
            await Context.Channel.SendMessageAsync("Skipping song!");
        }

        private readonly MusicPlayer _musicPlayer;
    }
}