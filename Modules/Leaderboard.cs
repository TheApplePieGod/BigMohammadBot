using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BigMohammadBot.Database.FunctionModels;

namespace BigMohammadBot.Modules
{
    public class Leaderboard : ModuleBase<SocketCommandContext>
    {
        
        [Command("top")]
        public async Task Task1()
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var Results = await dbContext.HelloMessageCountModel.FromSqlRaw(@"select UserId, 0 as Iteration, SUM(NumMessages) as NumMessages from udf_GetHelloMessageCount(@iteration, @alliterations, @userid, @allusers)
                                                                              group by UserId
                                                                              order by NumMessages desc",
                new SqlParameter("@iteration", Convert.ToInt32(0)),
                new SqlParameter("@alliterations", true),
                new SqlParameter("@userid", Convert.ToInt32(0)),
                new SqlParameter("@allusers", true)
            ).ToArrayAsync();

            if (Results.Length == 0)
                await ReplyAsync("Hmmm, I don't have any data for this command.");
            else
            {
                string LeaderboardString = "";
                for (int i = 0; i < Math.Min(10, Results.Length); i++)
                {
                    var User = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == Results[i].UserId).FirstOrDefaultAsync();
                    if (User != null)
                        LeaderboardString += (i + 1) + ". " + User.DiscordUserName + ": **" + Results[i].NumMessages + "**\n";
                }

                var embed = new EmbedBuilder
                {
                    Title = "Top 10 Hello Chain Messagers",
                    Description = "All Time"
                };
                embed.AddField("Messages", LeaderboardString);
                await ReplyAsync(embed: embed.Build());
            }
        }

        [Command("now")]
        public async Task Task2()
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

            var Results = await dbContext.HelloMessageCountModel.FromSqlRaw(@"select * from udf_GetHelloMessageCount(@iteration, @alliterations, @userid, @allusers) order by NumMessages desc",
                new SqlParameter("@iteration", AppState.HelloIteration),
                new SqlParameter("@alliterations", false),
                new SqlParameter("@userid", Convert.ToInt32(0)),
                new SqlParameter("@allusers", true)
            ).ToArrayAsync();

            if (Results.Length == 0)
                await ReplyAsync("Hmmm, I don't have any data for this command.");
            else
            {
                string LeaderboardString = "";
                for (int i = 0; i < Math.Min(5, Results.Length); i++)
                {
                    var User = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == Results[i].UserId).FirstOrDefaultAsync();
                    if (User != null)
                        LeaderboardString += (i + 1) + ". " + User.DiscordUserName + ": **" + Results[i].NumMessages + "**\n";
                }

                var embed = new EmbedBuilder
                {
                    Title = "Top 5 Hello Chain Messagers",
                    Description = "Current Iteration"
                };
                embed.AddField("Messages", LeaderboardString);
                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}
