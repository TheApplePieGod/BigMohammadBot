using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using BigMohammadBot.Database.FunctionModels;

namespace BigMohammadBot.Modules
{
    public class Break : ModuleBase<SocketCommandContext>
    {
        [Command("break")]
        public async Task Task2(string MentionedUser = "")
        {
            await ReplyAsync("This command has been replaced with $breakchain to prevent confusion");
        }

        [Command("breakchain")]
        public async Task Task1(string MentionedUser)
        {
            string FormattedId = new string(MentionedUser.Where(char.IsNumber).ToArray());
            var User = await Context.Client.Rest.GetGuildUserAsync(Globals.MohammadServerId, ulong.Parse(FormattedId));

            if (User == null)
                throw new Exception("User not found");

            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                Database.DatabaseContext dbContext = new Database.DatabaseContext();
                var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();
                var GeneralChannel = Context.Client.GetChannel(Globals.GeneralChannelId) as SocketTextChannel;
                int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);

                try
                {
                    var dbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(c => c.Id == AppState.HelloChannelId).FirstOrDefaultAsync();
                    var HelloChannel = Context.Client.GetChannel(dbChannel.DiscordChannelId.ToInt64()) as SocketTextChannel;
                    await HelloChannel.DeleteAsync();
                    dbChannel.Deleted = true;
                    Globals.LogActivity(4, "From command", "Iteration: " + AppState.HelloIteration, true, CallingUserId);
                }
                catch (Exception e)
                {
                    Globals.LogActivity(4, "From command", "Iteration: " + AppState.HelloIteration + " Error: " + e.Message, false, CallingUserId);
                    await ReplyAsync("Operation failed: " + e.Message);
                    throw new Exception("Operation failed: " + e.Message);
                }

                int dbUserId = await Globals.GetDbUserId(User);
                var dbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == dbUserId).FirstOrDefaultAsync();
                dbUser.ChainBreaks = dbUser.ChainBreaks + 1;

                try
                {
                    Globals.AwardChainKeeper(AppState.HelloIteration, dbUserId, Context.Guild, Context.Client);
                    Globals.SetSuspendedUser(dbUserId, Context.Guild, Context.Client);
                }
                catch (Exception e)
                {
                    Globals.LogActivity(1, "Failed updating roles after break", "Error: " + e.Message, false, CallingUserId);
                    await ReplyAsync("Failed to update roles");
                }

                AppState.HelloTimerNotified = false;
                if (AppState.AutoCreateNewHello)
                {
                    await GeneralChannel.SendMessageAsync("<@!" + Context.User.Id + "> has decided that <@!" + User.Id + "> has broken the chain. A new chain has been created");

                    try
                    {
                        AppState.HelloIteration = AppState.HelloIteration + 1;
                        var NewChannel = await Context.Guild.CreateTextChannelAsync("hello-chain-" + AppState.HelloIteration, x =>
                        {
                            x.CategoryId = Globals.HelloCategoryId;
                            x.Topic = AppState.HelloTopic;
                        });
                        AppState.HelloChannelId = await Globals.GetDbChannelId(NewChannel.Id, NewChannel.Name, 2);
                        AppState.HelloDeleted = false;
                        AppState.LastHelloMessage = DateTime.Now;
                        AppState.LastHelloUserId = 0;
                        Globals.LogActivity(5, "From command", "Iteration: " + AppState.HelloIteration, true, CallingUserId);
                    }
                    catch (Exception e)
                    {
                        Globals.LogActivity(5, "From command", "Iteration: " + AppState.HelloIteration + " Error: " + e.Message, false, CallingUserId);
                        await ReplyAsync("Operation failed: " + e.Message);
                        throw new Exception("Operation failed: " + e.Message);
                    }
                }
                else
                {
                    await GeneralChannel.SendMessageAsync("<@!" + Context.User.Id + "> has decided that <@!" + User.Id + "> has broken the chain. The channel has been deleted");
                    AppState.HelloDeleted = true;
                }

                var BreakerRole = Context.Guild.GetRole(Globals.ChainBreakerRoleId);
                await (User as IGuildUser).AddRoleAsync(BreakerRole);

                await dbContext.SaveChangesAsync();
                //await Context.Message.DeleteAsync();
            }
        }
    }
}
