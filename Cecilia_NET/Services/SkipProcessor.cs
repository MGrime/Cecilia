using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Cecilia_NET.Services
{
    // Service to manage any active skips
    public class SkipProcessor
    {
        private ActiveSkip _currentSkip;

        public SkipProcessor()
        {
            _currentSkip = null;
        }

        public async Task<bool> RegisterSkip(SocketCommandContext context, ulong voiceChannelId)
        {
            // Get voice channel
            var correctChannel = context.Guild.GetVoiceChannel(voiceChannelId);
            
            int votesNeeded = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(correctChannel.Users.Count()) / 1.0));
            // There isnt a skip already in motion
            if (_currentSkip == null)
            {
                // Set it up
                _currentSkip = new ActiveSkip {Votes = new List<SkipVote>()};
                
                // This person wants a skip so add them
                _currentSkip.Votes.Add(new SkipVote{VoterId = context.User.Id});

                // Return skip init message
                var response = Helpers.CeciliaEmbed(context);
                response.AddField("Skip request begun!", $"Let's hope people agree with you! Votes: {1}/{votesNeeded}");
                var message = await context.Channel.SendMessageAsync("", false, response.Build());
                _currentSkip.MessageToDelete = message;
            }
            else
            {
                bool hasVoted = false;
                // Check if this person has already voted
                foreach (var vote in _currentSkip.Votes)
                {
                    // They have already voted
                    if (vote.VoterId == context.User.Id)
                    {
                        hasVoted = true;
                        break;
                    }
                }

                if (!(hasVoted))
                {
                    // Add to the votes
                    _currentSkip.Votes.Add(new SkipVote{VoterId = context.User.Id});
                    
                }
                // Update message
                    
                // Check if anymore messages have been sent in the channel the skip message was sent in
                // get the latest message
                var lastMessages = await _currentSkip.MessageToDelete.Channel.GetMessagesAsync(1).FlattenAsync();
                var enumerable = lastMessages as IMessage[] ?? lastMessages.ToArray();
                enumerable.GetEnumerator().MoveNext();
                var lastMessage = enumerable[0];
                // Check if its the skip message
                if (lastMessage.Id == _currentSkip.MessageToDelete.Id)
                {
                    // Modify if it hasnt changed
                    var response = Helpers.CeciliaEmbed(context);
                    var fieldToAdd = $"Let's hope people agree with you! Votes: {1}/{votesNeeded}";
                    // compare string field values
                    if (!_currentSkip.MessageToDelete.Embeds.ToList()[0].Fields[0].Value.Equals(fieldToAdd))
                    {
                        response.AddField("Skip request begun!", fieldToAdd);
                        await _currentSkip.MessageToDelete.ModifyAsync(m =>{m.Embed = response.Build();});
                    }
                }
                else
                {
                    // Save old embed
                    var oldEmbed = _currentSkip.MessageToDelete.Embeds.ToList()[0];
                    // Delete and send new
                    Helpers.DeleteCommand(context,_currentSkip.MessageToDelete.Id);
                        
                    var response = Helpers.CeciliaEmbed(context);
                    response.WithFooter(oldEmbed.Footer.ToString());
                    response.AddField("Skip request begun!", $"Let's hope people agree with you! Votes: {1}/{votesNeeded}");
                    var newMessage = await _currentSkip.MessageToDelete.Channel.SendMessageAsync("", false, response.Build());
                    _currentSkip.MessageToDelete = newMessage;
                        
                }
            }
            

            // If there are more skips votes than half of the amount of users in the channel
            // -1 because dont count the bot
            if (_currentSkip.Votes.Count >= votesNeeded)
            {
                // Skip
                ClearSkip(context);
                // This triggers skip
                return true;
            }
            // Dont skip
            return false;
        }

        // Clear skip on leave or new song
        public void ClearSkip(SocketCommandContext context)
        {
            if (_currentSkip != null)
            {
                Helpers.DeleteCommand(context,_currentSkip.MessageToDelete.Id);
            }
            _currentSkip = null;
        }
        

        public ActiveSkip CurrentSkip
        {
            get => _currentSkip;
            set => _currentSkip = value;
        }

        // Convenience wrapper
        public class ActiveSkip
        {
            public IUserMessage MessageToDelete { get; set; }
            public List<SkipVote> Votes { get; set; }
        }

        public class SkipVote
        {
            public ulong VoterId { get; set; }
        }
        
    }
}