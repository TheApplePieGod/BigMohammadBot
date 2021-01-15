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

public struct UserPercent
{
    public string UserName { get; set; }
    public decimal Percent { get; set; }
}

namespace BigMohammadBot.Modules
{
    public class Report : ModuleBase<SocketCommandContext>
    {
        public class ReportReturn
        {
            public int TotalMessages;
            public int TotalSeconds;
            public string Data;
        }

        public async Task<ReportReturn> ReportRange(string Bottom, string Top)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            int TotalMessages = 0;
            int TotalSecondsInVoice = 0;

            var AllUserActivity = await dbContext.UserActivityModel.FromSqlRaw(@"select UserName, sum(TotalMessages) as TotalMessages, sum(TotalSecondsInVoice) as TotalSecondsInVoice from udf_GetUserActivity(@userid, @allusers, @timebottom, @timetop) group by UserName",
                new SqlParameter("@userid", 1),
                new SqlParameter("@allusers", true),
                new SqlParameter("@timebottom", Bottom),
                new SqlParameter("@timetop", Top)
            ).ToListAsync();
            foreach (UserActivity Stat in AllUserActivity)
            {
                TotalMessages += Stat.TotalMessages;
                TotalSecondsInVoice += Stat.TotalSecondsInVoice;
            }

            ReportReturn Data = new ReportReturn();
            Data.TotalMessages = TotalMessages;
            Data.TotalSeconds = TotalSecondsInVoice;
            Data.Data = "";

            if (TotalMessages == 0 && TotalSecondsInVoice == 0)
                return Data;

            List<UserPercent> UserPercents = new List<UserPercent>();
            foreach (UserActivity Stat in AllUserActivity)
            {
                float MessageAverage = (float)Stat.TotalMessages / (float)Math.Max(TotalMessages, 1);
                float VoiceAverage = (float)Stat.TotalSecondsInVoice / (float)Math.Max(TotalSecondsInVoice, 1);

                double WeightedAverage = 0;
                if (TotalMessages == 0)
                    WeightedAverage = VoiceAverage * 100;
                else if (TotalSecondsInVoice == 0)
                    WeightedAverage = MessageAverage * 100;
                else
                    WeightedAverage = ((MessageAverage * 0.6) + (VoiceAverage * 0.4)) / 1 * 100;

                UserPercent Output = new UserPercent();
                Output.UserName = Stat.UserName;
                Output.Percent = Decimal.Round((decimal)WeightedAverage, 1);
                UserPercents.Add(Output);
            }

            string ReportString = "";
            UserPercents = UserPercents.OrderByDescending(e => e.Percent).ToList();
            foreach (UserPercent Element in UserPercents)
            {
                ReportString += Element.UserName + ": **" + Element.Percent + "%**\n";
            }

            Data.Data = ReportString;
            return Data;
        }

        public EmbedBuilder CreateEmbed(ReportReturn ReportData, string Bottom, string Top)
        {
            string WeekString = "";
            if (Bottom == "" && Top == "")
                WeekString = "All Time";
            else if (Bottom == Top)
                WeekString = "Week of " + Bottom;
            else
                WeekString = "From " + Bottom + " To " + Top;

            var embed = new EmbedBuilder
            {
                Title = "Activity report",
                Description = WeekString + "\nTotal messages: " + ReportData.TotalMessages + "\nTotal minutes in voice: " + Decimal.Round((decimal)ReportData.TotalSeconds / 60, 1)
            };
            embed.AddField("Activity percentage (users):", ReportData.Data);
            return embed;
        }

        [Command("report")]
        public async Task Task1()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                DateTime LastWeekDate = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 7) + (int)DayOfWeek.Monday);
                ReportReturn ReportData = await ReportRange(LastWeekDate.ToString(), LastWeekDate.ToString());
                if (ReportData.Data == "")
                    await ReplyAsync("No data to report from last week");
                else
                    await ReplyAsync(embed: CreateEmbed(ReportData, LastWeekDate.ToShortDateString(), LastWeekDate.ToShortDateString()).Build());
            }
        }

        [Command("reportnow")]
        public async Task Task2()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                DateTime ThisWeekDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                ReportReturn ReportData = await ReportRange(ThisWeekDate.ToString(), ThisWeekDate.ToString());
                if (ReportData.Data == "")
                    await ReplyAsync("No data to report from this week");
                else
                    await ReplyAsync(embed: CreateEmbed(ReportData, ThisWeekDate.ToShortDateString(), ThisWeekDate.ToShortDateString()).Build());
            }
        }

        [Command("reportall")]
        public async Task Task3()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                ReportReturn ReportData = await ReportRange("", "");
                if (ReportData.Data == "")
                    await ReplyAsync("No data to report");
                else
                    await ReplyAsync(embed: CreateEmbed(ReportData, "", "").Build());
            }
        }
    }
}
