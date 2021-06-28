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
    public class CreateHello : ModuleBase<SocketCommandContext>
    {
        [Command("createhello")]
        public async Task Task1()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.ManageMessages && !Globals.AdminUserIds.Contains(Context.Message.Author.Id)) // chat moderator
                throw new Exception("You do not have permission to run that command");
            else
            {
                var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
                var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

                if (!AppState.EnableHelloChain)
                    throw new Exception("The [Hello Chain] feature is not enabled");

                int CallingUserId = await Globals.GetDbUserId(Context.Guild.Id, Context.Message.Author);
                var ResponseChannel = Context.Guild.DefaultChannel;
                if (AppState.ResponseChannelId != null && AppState.ResponseChannelId.Length > 0)
                    ResponseChannel = Context.Client.GetChannel(AppState.ResponseChannelId.ToInt64()) as SocketTextChannel;

                if (!AppState.HelloDeleted.Value)
                    throw new Exception("A new chain cannot be created until the last one is broken");
                else
                {
                    Discord.Rest.RestTextChannel NewChannel = null;
                    AppState.HelloTimerNotified = false;
                    try
                    {
                        AppState.HelloIteration = AppState.HelloIteration + 1;
                        NewChannel = await Context.Guild.CreateTextChannelAsync("hello-chain-" + AppState.HelloIteration, x =>
                        {
                            if (AppState.HelloCategoryId != null && AppState.HelloCategoryId.Length > 0)
                                x.CategoryId = AppState.HelloCategoryId.ToInt64();
                            x.Topic = AppState.HelloTopic;
                        });
                        AppState.HelloChannelId = await Globals.GetDbChannelId(Context.Guild.Id, NewChannel.Id, NewChannel.Name, 2);
                        AppState.HelloDeleted = false;
                        AppState.LastHelloMessage = DateTime.Now;
                        AppState.LastHelloUserId = 0;
                        Globals.SetSuspendedUser(ResponseChannel, 0, Context.Guild, Context.Client);
                        Globals.LogActivity(Context.Guild.Id, 5, "From command", "Iteration: " + AppState.HelloIteration, true, CallingUserId);
                    }
                    catch (Exception e)
                    {
                        Globals.LogActivity(Context.Guild.Id, 5, "From command", "Iteration: " + AppState.HelloIteration + " Error: " + e.Message, false, CallingUserId);
                        await ReplyAsync("Operation failed: " + e.Message);
                        throw new Exception("Operation failed: " + e.Message);
                    }

                    await dbContext.SaveChangesAsync();
                    //await Context.Message.DeleteAsync();
                    await ReplyAsync("Successfully created channel <#" + NewChannel.Id + ">");

                    Globals.LogActivity(Context.Guild.Id, 7, "", "Successfully reset channel to " + NewChannel.Name, true, CallingUserId);
                }
            }
        }
    }
}
