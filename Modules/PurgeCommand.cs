using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace BigMohammadBot.Modules
{
    public class PurgeCommand : ModuleBase<SocketCommandContext>
    {
        
        [Command("purge")]
        public async Task Task1([Remainder] int numDelete = 0) //place parameters for command in these ()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                if (numDelete == 0)
                    await Context.Channel.SendMessageAsync("Please put the number of messages to delete. '$purge (amount)'");
                else
                {
                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(numDelete + 1).FlattenAsync();
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
                    await Context.Channel.SendMessageAsync("Successfully deleted " + numDelete + " messages");
                }
            }
        }
    }
}
