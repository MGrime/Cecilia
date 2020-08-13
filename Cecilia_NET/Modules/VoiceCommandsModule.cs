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
using Embed = Discord.Embed;


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
            var response = Helpers.CeciliaEmbed(Context);
            if (Context.Guild.AudioClient != null && Context.Guild.AudioClient.ConnectionState != ConnectionState.Disconnected)
            {
                response.AddField("I'm already connected!", "Drag me to a different room if you want to switch.");
                await Context.Channel.SendMessageAsync("",false,response.Build());
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
                response.AddField("Please first join a channel! ", "Alternatively specify a valid channel as an argument.");
                await Context.Channel.SendMessageAsync("",false,response.Build());
                    // Delete the user command
                Helpers.DeleteUserCommand(Context);

                return;
            }
            
            // Now we have a valid context, channel and guild
            var client = await channel.ConnectAsync(true);
            
            _musicPlayer.RegisterAudioClient(Context.Guild.Id,client);

            // Display connection message
            response.AddField("I've Connected!", "Thanks for inviting me!",true);
            await Context.Channel.SendMessageAsync("",false,response.Build());

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
            var response = Helpers.CeciliaEmbed(Context);
            response.AddField("I'm off!", "See you next time!");
            await Context.Channel.SendMessageAsync("",false,response.Build());
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Adds a song to the play queue")]
        public async Task PlayAsync([Remainder] [Summary("The URL to play.")] string uri)
        {
            // Check if connected to voice
            var response = Helpers.CeciliaEmbed(Context);
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
                    response.AddField("I'm not connected!", "Join a voice channel and re-run the command.");
                    await Context.Channel.SendMessageAsync("", false, response.Build());

                }
                // Then continue if connected
            }
            
            // Delete user command if not deleted by join
            if (Context.Channel.GetMessageAsync(Context.Message.Id).Result != null)
            {
                Helpers.DeleteUserCommand(Context);
            }
            
            // Send a searching embed
            response.AddField("Searching...", "Give me a minute to look that up!");
            var searchEmbed = await Context.Channel.SendMessageAsync("",false,response.Build());

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
            
            // 3. Add to queue
            await Context.Channel.DeleteMessageAsync(searchEmbed.Id);
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
            await Bot.CreateLogEntry(LogSeverity.Info, "Commands", "Skip Requested!");
            // Set skip boolean
            _musicPlayer.ActiveAudioClients[Context.Guild.Id].Skip = true;
            // Output skipping song message
            var response = Helpers.CeciliaEmbed(Context);
            if (_musicPlayer.ActiveAudioClients[Context.Guild.Id]?.Queue.Count > 1)
            {
                response.AddField("Skipping Song!", "Onto the next one...");
            }
            else
            {
                response.AddField("Skipping Song!", "Spin up some more songs with the play command!");
            }
            await Context.Channel.SendMessageAsync("", false, response.Build());
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
                await Bot.CreateLogEntry(LogSeverity.Info, "Commands", "Pause Requested!");
                // Set paused boolean
                _musicPlayer.ActiveAudioClients[Context.Guild.Id].Paused = true;
                // output paused message
                var response = Helpers.CeciliaEmbed(Context);
                response.AddField("Pausing playback!", "Take a break and recharge!");
                await Context.Channel.SendMessageAsync("", false, response.Build());
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
                await Bot.CreateLogEntry(LogSeverity.Info, "Commands", "Resume Requested!");
                // Unset pause boolean
                _musicPlayer.ActiveAudioClients[Context.Guild.Id].Paused = false;
                // Send message
                var response = Helpers.CeciliaEmbed(Context);
                response.AddField("Resuming playback!", "Here come the bangers!");
                await Context.Channel.SendMessageAsync("", false, response.Build());
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
            var embedBuilder = Helpers.CeciliaEmbed(Context);
            if (audioClient == null || audioClient.Client.ConnectionState == ConnectionState.Disconnected ||
                audioClient.Queue.Count == 0)
            {
                // Say no queue and leave
                embedBuilder.AddField("The queue is empty!", "Add some songs with the play command!");

                await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
                
                // Delete message
                Helpers.DeleteUserCommand(Context);

                return;
            }
            
            // There is a connection & queue
            int queuePosition = 1;
            embedBuilder.WithTitle($"Current Queue (Up to {Helpers.MAX_FIELD_IN_EMBED})");
            foreach (var item in audioClient.Queue)
            {
                // building explicitly due to size for readability
                var video = item.Item2;
                
                string fieldValue = "";
                var correctedSeconds = video.Duration.Seconds <= 9
                    ? $"0{video.Duration.Seconds}"
                    : video.Duration.Seconds.ToString();
                fieldValue += " Length: " + video.Duration.Minutes + ":" + correctedSeconds;
                fieldValue += $" [View on YT]({video.Url})";
                
                embedBuilder.AddField($"{queuePosition}: {video.Title}" , fieldValue);
                ++queuePosition;
                // Max queue embed size
                if (queuePosition == Helpers.MAX_FIELD_IN_EMBED)
                {
                    break;
                }
            }
            
            await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
            // Delete message
            Helpers.DeleteUserCommand(Context);
        }

        private readonly MusicPlayer _musicPlayer;
    }
}