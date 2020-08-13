using Discord.WebSocket;

namespace Cecilia_NET
{
    // Collection of functions that don't fit into class
    public static class Helpers
    {
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
    }
}