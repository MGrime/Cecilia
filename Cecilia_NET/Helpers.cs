using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SpotifyAPI.Web;
using TagLib.Riff;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Cecilia_NET
{
    // Collection of functions that don't fit into class
    public static class Helpers
    {
        // THIS COMES FROM THE DISCORD API
        public const int MAX_FIELD_IN_EMBED = 25;
        
        // Gets a display name. Username if the user doesnt have a nickname in the guild they are running the command in. Else nickname
        public static string GetDisplayName(SocketUser user)
        {
            var guildUser = user as SocketGuildUser;
            if (guildUser == null)
            {
                return user.Username;
            }
            else
            {
                return guildUser.Nickname ?? guildUser.Username;
            }
        }

        // Delete the message that sent the command
        public static async void DeleteUserCommand(SocketCommandContext context)
        {
            if (context.Channel.GetMessageAsync(context.Message.Id).Result == null) return;
            
            await context.Channel.DeleteMessageAsync(context.Message.Id);

        }
    
        // Remove characters that could break filenames & paths
        public static string ProcessVideoTitle(string videoTitle)
        {
            const char replacementChar = '-';

            // Remove forward slashes
            var output = videoTitle.Replace('/', replacementChar);
            // remove back slashes
            output = output.Replace('\\', replacementChar);
            // Remove all colons
            output = output.Replace(':', replacementChar);
            // Remove all asterisks
            output = output.Replace('*', replacementChar);
            // Remove all question marks
            output = output.Replace('?', replacementChar);
            // Remove all double quotes
            output = output.Replace('\"', replacementChar);
            // Remove all left chevrons
            output = output.Replace('<', replacementChar);
            // Remove all right chevrons
            output = output.Replace('>', replacementChar);
            // Remove all left graves
            output = output.Replace('|', replacementChar);

            return output;
        }
        
        // Provide a shell embed builder with cecilia branding and requesting user
        // Adds the author and footer
        public static EmbedBuilder CeciliaEmbed(SocketCommandContext context)
        {
            var outBuilder = new EmbedBuilder();
            outBuilder.WithAuthor(context.Client.CurrentUser.Username, context.Client.CurrentUser.GetAvatarUrl());
            var correctedMinutes = DateTime.Now.Minute <= 9 ? $"0{DateTime.Now.Minute}" : DateTime.Now.Minute.ToString();
            outBuilder.WithFooter($"Requested by {GetDisplayName(context.User)} @ {DateTime.Now.Hour}:{correctedMinutes}");
            return outBuilder;
        }

        public static async Task DownloadSong(IStreamInfo streamInfo, string filePath)
        {
            var youtube = new YoutubeClient();
            await youtube.Videos.Streams.DownloadAsync(streamInfo, filePath);
        }

        public static async Task<System.Collections.Generic.List<FullTrack>> SpotifyQuery(string searchTerms)
        {
            // This is not perfect but should help with things like BAND - SONG (live in yada yada)

            if (searchTerms.Contains('('))
            {
                searchTerms = searchTerms.Remove(searchTerms.IndexOf('('));
            }

            if (searchTerms.Contains('['))
            {
                searchTerms = searchTerms.Remove(searchTerms.IndexOf('['));
            }

            // If they haven't provided a client then leave
            if (Bot.SpotifyConfig == null) return null;
            
            var spotify = new SpotifyClient(Bot.SpotifyConfig);
            SearchResponse search;
            try
            {
                search = await spotify.Search.Item(new SearchRequest(SpotifyAPI.Web.SearchRequest.Types.Track, searchTerms));
            }
            catch (Exception e)
            {
                await Bot.CreateLogEntry(LogSeverity.Warning, "Spotify", e.Message);
                return null;
            }

            if (search.Tracks.Items?.Count == 0) return null;
            else return search.Tracks.Items;
        }
        
    }
}