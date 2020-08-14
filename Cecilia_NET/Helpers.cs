using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using YoutubeExplode;
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
            try
            {
                await context.Channel.DeleteMessageAsync(context.Message.Id);
            }
            catch (Exception e)
            {
                await Bot.CreateLogEntry(LogSeverity.Warning, "MSGDeletion", e.ToString());
            }
            
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
    }
}