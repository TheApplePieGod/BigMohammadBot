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
    public class ChainLength : ModuleBase<SocketCommandContext>
    {
        
        [Command("chainlength")]
        public async Task Task1(int iteration = 0)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

            int realIteration = iteration == 0 ? AppState.HelloIteration : iteration;
            var Count = await dbContext.IterationCountModel.FromSqlRaw(@"select * from udf_GetIterationMessageCount(@iteration)",
                new SqlParameter("@iteration", realIteration)
            ).FirstOrDefaultAsync();

            if (Count.Count == 0)
                await ReplyAsync("Hmmm, I don't have any data for that chain.");
            else
            {
                if (realIteration == AppState.HelloIteration)
                    await ReplyAsync("The length of the current chain is **" + Count.Count + "** greeting(s).");
                else
                    await ReplyAsync("The length of chain " + iteration + " was **" + Count.Count + "** greeting(s).");
            }
        }

        [Command("chains")]
        public async Task Task2()
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var Count = await dbContext.IterationCountModel.FromSqlRaw(@"select * from udf_GetIterationMessageCount(@iteration)",
                new SqlParameter("@iteration", Convert.ToInt32(0))
            ).ToListAsync();

            string LeaderboardString = "";
            for (int i = 0; i < Math.Min(5, Count.Count); i++)
            {
                LeaderboardString += (i + 1) + ". Chain " + Count[i].Iteration + ": **" + Count[i].Count + "**\n";
            }

            var embed = new EmbedBuilder
            {
                Title = "Top 5 Hi Chains",
                Description = ""
            };
            embed.AddField("Messages", LeaderboardString);
            await ReplyAsync(embed: embed.Build());
        }
    }
}
