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
    public class MessageChannel : ModuleBase<SocketCommandContext>
    {
        [Command("messagechannel")]
        public async Task Task1(ulong ChannelId, string Message)
        {
            if (Globals.AdminUserIds.Contains(Context.Message.Author.Id)) // admin command
            {
                var Channel = Context.Client.GetChannel(ChannelId) as ITextChannel;
                await Channel.SendMessageAsync(Message);
                await ReplyAsync("Sent");
            }
        }
    }
}
