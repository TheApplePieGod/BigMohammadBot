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
                Database.DatabaseContext dbContext = new Database.DatabaseContext();
                var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();
                int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);

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
                            x.CategoryId = Globals.HelloCategoryId;
                            x.Topic = AppState.HelloTopic;
                        });
                        AppState.HelloChannelId = await Globals.GetDbChannelId(NewChannel.Id, NewChannel.Name, 2);
                        AppState.HelloDeleted = false;
                        AppState.LastHelloMessage = DateTime.Now;
                        AppState.LastHelloUserId = 0;
                        Globals.SetSuspendedUser(0, Context.Guild, Context.Client);
                        Globals.LogActivity(5, "From command", "Iteration: " + AppState.HelloIteration, true, CallingUserId);
                    }
                    catch (Exception e)
                    {
                        Globals.LogActivity(5, "From command", "Iteration: " + AppState.HelloIteration + " Error: " + e.Message, false, CallingUserId);
                        await ReplyAsync("Operation failed: " + e.Message);
                        throw new Exception("Operation failed: " + e.Message);
                    }

                    await dbContext.SaveChangesAsync();
                    //await Context.Message.DeleteAsync();
                    await ReplyAsync("Successfully created channel <#" + NewChannel.Id + ">");

                    Globals.LogActivity(7, "", "Successfully reset channel to " + NewChannel.Name, true, CallingUserId);
                }
            }
        }
    }
}
