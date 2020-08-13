using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cecilia_NET.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YouTubeSearch;


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
                // Delete the user command
                Helpers.DeleteUserCommand(Context);

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
                // Delete the user command
                Helpers.DeleteUserCommand(Context);

                return;
            }
            
            // Now we have a valid context, channel and guild
            var client = await channel.ConnectAsync(true);
            
            _musicPlayer.RegisterAudioClient(Context.Guild.Id,client);

            // Display connection message
            await Context.Channel.SendMessageAsync("I've connected! Thanks for inviting me, " + Helpers.GetDisplayName(Context.User) + "!");

            // Delete the user command
            Helpers.DeleteUserCommand(Context);
        }

        [Command("leave",RunMode = RunMode.Async)]
        [Summary("Leaves the voice channel if we are connected to one")]
        public async Task LeaveAsync()
        {
            // Delete the user command
            Helpers.DeleteUserCommand(Context);

            // Check for connection
            if (Context.Guild.AudioClient == null || Context.Guild.AudioClient.ConnectionState == ConnectionState.Disconnected)
            {
                return;
            }
            // There is a connect in this guild
            // Disconnect
            await Context.Guild.AudioClient.StopAsync();
            _musicPlayer.RemoveAudioClient(Context.Guild.Id);
            
            // Cleanup audio cache
            // If there are no active clients
            if (_musicPlayer.ActiveAudioClients.Count == 0)
            {
                // Get correct directory
                var directoryPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"AudioCache\" : "AudioCache/";

                // Close file streams so files can be deleted
                _musicPlayer.CloseFileStreams();

                // Clear it
                foreach (var file in Directory.GetFiles(directoryPrefix))
                {
                    File.Delete(file);
                }
            }
            
            // Now we have disconnected
            await Context.Channel.SendMessageAsync("I'm off! Cya next time! (Removed by " + Helpers.GetDisplayName(Context.User) + ")");
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

            // Calculate correct directory prefix for different OS
            var directoryPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"AudioCache\" : "AudioCache/";
            
            // Start youtube explode
            var youtube = new YoutubeClient();
            // Get video metadata & download thumbnail
            Video video;
            try
            {
                video = await youtube.Videos.GetAsync(uri);
            }
            catch(ArgumentException e)
            {
                // This means that it is not a uri it is a search term.
                // So search videos
                var items = new VideoSearch();
                // Get the first page of videos
                var videos = await items.GetVideos(uri, 1);
                // find the top one
                var topVideo = videos.First();
                video = await youtube.Videos.GetAsync(topVideo.getUrl());
            }
            
            // Process correct title
            var processedTitle = Helpers.ProcessVideoTitle(video.Title);
            
            // Check if file exists. If it does skip the download
            // TODO: This sucks. Make it more efficient - Michael
            bool fileExists = false;
            foreach (var file in Directory.GetFiles(directoryPrefix))
            {
                if (file == $"{processedTitle}.mp3")
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
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{directoryPrefix}{processedTitle}.mp3");
                }
            }

            // Delete user command if not deleted by join
            if (Context.Channel.GetMessageAsync(Context.Message.Id).Result != null)
            {
                Helpers.DeleteUserCommand(Context);
            }

            // 3. Add to queue
            EmbedBuilder builder = new EmbedBuilder();
            _musicPlayer.AddSongToQueue(Context,$"{directoryPrefix}{processedTitle}.mp3",video, ref builder);

            // 4. Notify added
            await Context.Channel.SendMessageAsync("", false, builder.Build());
            
            // Now play
            await _musicPlayer.PlayAudio(Context);
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Summary("Skips current song")]
        public async Task SkipAsync()
        {
            // Log skip
            Console.WriteLine("Skip requested!");
            // Set skip boolean
            _musicPlayer.ActiveAudioClients[Context.Guild.Id].Skip = true;
            // Output skipping song message
            await Context.Channel.SendMessageAsync("Skipping song! (Requested by " + Helpers.GetDisplayName(Context.User) + ")");
            // Delete the user command
            Helpers.DeleteUserCommand(Context);
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Summary("Pauses playback")]
        public async Task PauseAsync()
        {
            // Check we aren't already paused
            if (!_musicPlayer.ActiveAudioClients[Context.Guild.Id].Paused)
            {
                // Log
                Console.WriteLine("Pause requested!");
                // Set paused boolean
                _musicPlayer.ActiveAudioClients[Context.Guild.Id].Paused = true;
                // output paused message
                await Context.Channel.SendMessageAsync("Pausing playback! (Requested by " + Helpers.GetDisplayName(Context.User) + ")");
                // Delete the user command
                Helpers.DeleteUserCommand(Context);
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Resumes playback")]
        public async Task ResumeAsync()
        {
            // Check we are paused
            if (_musicPlayer.ActiveAudioClients[Context.Guild.Id].Paused)
            {
                // Log
                Console.WriteLine("Resume requested!");
                // Unset pause boolean
                _musicPlayer.ActiveAudioClients[Context.Guild.Id].Paused = false;
                // Send message
                await Context.Channel.SendMessageAsync("Resuming playback! (Requested by " + Helpers.GetDisplayName(Context.User) + ")");
                // Delete the user command
                Helpers.DeleteUserCommand(Context);
            }
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Summary("List the queued songs")]
        public async Task QueueAsync()
        {
            // Get the client for this server
            MusicPlayer.WrappedAudioClient audioClient;
            try
            {
                audioClient = _musicPlayer.ActiveAudioClients[Context.Guild.Id];
            }
            catch (KeyNotFoundException e)
            {
                audioClient = null;
            }
            // If there is no connection, or we are disconnected, or there is no queue
            if (audioClient == null || audioClient.Client.ConnectionState == ConnectionState.Disconnected ||
                audioClient.Queue.Count == 0)
            {
                // Say no queue and leave
                var embedBuilder = Helpers.CeciliaEmbed(Context);
                embedBuilder.WithTitle("The queue is empty!");
                embedBuilder.AddField(":-(", $"Add some songs with the play command!");

                await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
                
                // Delete message
                Helpers.DeleteUserCommand(Context);
            }
        }

        private readonly MusicPlayer _musicPlayer;
    }
}