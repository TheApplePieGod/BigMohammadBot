using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace BigMohammadBot.Modules
{
    public class ResetHello : ModuleBase<SocketCommandContext>
    {
        [Command("resethello")]
        public async Task Task1(SocketTextChannel NewChannel)
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
                var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

                if (!AppState.EnableHelloChain)
                    throw new Exception("The [Hello Chain] feature is not enabled");

                var ResponseChannel = Context.Guild.DefaultChannel;
                if (AppState.ResponseChannelId != null && AppState.ResponseChannelId.Length > 0)
                    ResponseChannel = Context.Client.GetChannel(AppState.ResponseChannelId.ToInt64()) as SocketTextChannel;

                AppState.HelloChannelId = await Globals.GetDbChannelId(NewChannel);
                AppState.HelloDeleted = false;
                AppState.HelloTimerNotified = false;
                AppState.LastHelloUserId = 0;
                AppState.LastHelloMessage = DateTime.Now;
                AppState.HelloIteration = AppState.HelloIteration + 1;
                await dbContext.SaveChangesAsync();
                //await Context.Message.DeleteAsync();

                Globals.SetSuspendedUser(ResponseChannel, 0, Context.Guild, Context.Client);

                await ReplyAsync("Successfully reset channel to <#" + NewChannel.Id + ">");

                int CallingUserId = await Globals.GetDbUserId(Context.Guild.Id, Context.Message.Author);
                Globals.LogActivity(Context.Guild.Id, 7, "", "Successfully reset channel to " + NewChannel.Name, true, CallingUserId);
            }
        }
    }
}
