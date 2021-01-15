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
    public class UserStats : ModuleBase<SocketCommandContext>
    {
        [Command("breakers")]
        public async Task Task1()
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var Count = await dbContext.ChainBreakCountModel.FromSqlRaw(@"select * from udf_GetChainBreaks(@userid)",
                new SqlParameter("@userid", Convert.ToInt32(0))
            ).ToListAsync();

            string LeaderboardString = "";
            for (int i = 0; i < Math.Min(5, Count.Count); i++)
            {
                LeaderboardString += (i + 1) + ". " + Count[i].UserName + ": **" + Count[i].Count + "**\n";
            }

            var embed = new EmbedBuilder
            {
                Title = "Top 5 Breakers",
                Description = ""
            };
            embed.AddField("Amount", LeaderboardString);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("keepers")]
        public async Task Task2()
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var Count = await dbContext.ChainBreakCountModel.FromSqlRaw(@"select * from udf_GetKeeperCount(@userid)",
                new SqlParameter("@userid", Convert.ToInt32(0))
            ).ToListAsync();

            string LeaderboardString = "";
            for (int i = 0; i < Math.Min(5, Count.Count); i++)
            {
                LeaderboardString += (i + 1) + ". " + Count[i].UserName + ": **" + Count[i].Count + "**\n";
            }

            var embed = new EmbedBuilder
            {
                Title = "Top 5 Keepers",
                Description = ""
            };
            embed.AddField("Amount", LeaderboardString);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("me")]
        public async Task Task3()
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            int userId = await Globals.GetDbUserId(Context.User);

            var BreakCount = await dbContext.ChainBreakCountModel.FromSqlRaw(@"select * from udf_GetChainBreaks(@userid)",
                new SqlParameter("@userid", userId)
            ).FirstOrDefaultAsync();

            var KeepCount = await dbContext.ChainBreakCountModel.FromSqlRaw(@"select * from udf_GetKeeperCount(@userid)",
                new SqlParameter("@userid", userId)
            ).FirstOrDefaultAsync();

            var TotalMessages = await dbContext.UserTotalMessagesModel.FromSqlRaw(@"select * from udf_GetUserTotalMessages(@userid, @allusers, @timebottom, @timetop)",
                new SqlParameter("@userid", userId),
                new SqlParameter("@allusers", false),
                new SqlParameter("@timebottom", ""),
                new SqlParameter("@timetop", "")
            ).FirstOrDefaultAsync();

            var TotalVoice = await dbContext.UserTotalVoiceTimeModel.FromSqlRaw(@"select * from udf_GetUserTotalVoiceTime(@userid, @allusers, @timebottom, @timetop)",
                new SqlParameter("@userid", userId),
                new SqlParameter("@allusers", false),
                new SqlParameter("@timebottom", ""),
                new SqlParameter("@timetop", "")
            ).FirstOrDefaultAsync();

            var embed = new EmbedBuilder
            {
                Title = "User Stats",
                Description = "Stats for **" + Context.User.Username + "**"
            };
            embed.AddField("Breaks", "Amount: **" + BreakCount.Count + "**");
            embed.AddField("Keeper of the Chain", "Amount: **" + KeepCount.Count + "**");
            embed.AddField("Activity All Time", "Total messages sent: **" + TotalMessages.TotalMessages + "**\nTotal minutes in voice: **" + Decimal.Round((decimal)TotalVoice.TotalSecondsInVoice / 60, 1) + "**");
            await ReplyAsync(embed: embed.Build());
        }
    }
}
