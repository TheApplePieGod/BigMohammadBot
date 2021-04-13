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
    public class DumpStats : ModuleBase<SocketCommandContext>
    {
        public class StatsReturn
        {
            public string MessagesString;
            public string VoiceChannelString;
            public string ChannelsString;
        }

        public async Task<StatsReturn> GetStatsFromRange(string Bottom, string Top)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            string MessagesString = "";
            var AllUserTotalMessages = await dbContext.UserTotalMessagesModel.FromSqlRaw(@"select * from udf_GetUserTotalMessages(@userid, @allusers, @timebottom, @timetop) order by TotalMessages desc",
                new SqlParameter("@userid", 1),
                new SqlParameter("@allusers", true),
                new SqlParameter("@timebottom", Bottom),
                new SqlParameter("@timetop", Top)
            ).ToListAsync();
            AllUserTotalMessages = AllUserTotalMessages.OrderByDescending(e => e.TotalMessages).ToList();
            foreach (UserTotalMessages Stat in AllUserTotalMessages)
            {
                MessagesString += Stat.UserName.Replace("/", "").Replace("\\", "") + ": **" + Stat.TotalMessages + "**\n";
            }

            string VoiceChannelString = "";
            var AllTotalVoiceSeconds = await dbContext.UserTotalVoiceTimeModel.FromSqlRaw(@"select * from udf_GetUserTotalVoiceTime(@userid, @allusers, @timebottom, @timetop) order by TotalSecondsInVoice desc",
                new SqlParameter("@userid", 1),
                new SqlParameter("@allusers", true),
                new SqlParameter("@timebottom", Bottom),
                new SqlParameter("@timetop", Top)
            ).ToListAsync();
            AllTotalVoiceSeconds = AllTotalVoiceSeconds.OrderByDescending(e => e.TotalSecondsInVoice).ToList();
            foreach (UserTotalVoiceTime Stat in AllTotalVoiceSeconds)
            {
                VoiceChannelString += Stat.UserName.Replace("/", "").Replace("\\", "") + ": **" + Decimal.Round((decimal)Stat.TotalSecondsInVoice / 60, 1) + "**\n";
            }

            string ChannelsString = "";
            var AllChannelTotalMessages = await dbContext.ChannelTotalMessagesModel.FromSqlRaw(@"select * from udf_GetChannelTotalMessages(@channelid, @allchannels, @timebottom, @timetop) order by TotalMessages desc",
                new SqlParameter("@channelid", 1),
                new SqlParameter("@allchannels", true),
                new SqlParameter("@timebottom", Bottom),
                new SqlParameter("@timetop", Top)
            ).ToListAsync();
            AllChannelTotalMessages = AllChannelTotalMessages.OrderByDescending(e => e.TotalMessages).ToList();
            foreach (ChannelTotalMessages Stat in AllChannelTotalMessages)
            {
                ChannelsString += Stat.ChannelName + ": **" + Stat.TotalMessages + "**\n";
            }

            if (MessagesString.Length > 1024)
                MessagesString = MessagesString.Substring(0, 1021) + "...";
            if (ChannelsString.Length > 1024)
                ChannelsString = ChannelsString.Substring(0, 1021) + "...";
            if (VoiceChannelString.Length > 1024)
                VoiceChannelString = VoiceChannelString.Substring(0, 1021) + "...";

            StatsReturn Data = new StatsReturn();
            Data.MessagesString = MessagesString;
            Data.ChannelsString = ChannelsString;
            Data.VoiceChannelString = VoiceChannelString;
            return Data;
        }

        public EmbedBuilder CreateEmbed(StatsReturn StatsData, string Bottom, string Top)
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
                Title = "Server statistics",
                Description = WeekString
            };
            if (StatsData.MessagesString != "")
                embed.AddField("Messages sent (users):", StatsData.MessagesString);
            if (StatsData.ChannelsString != "")
                embed.AddField("Messages sent (channels):", StatsData.ChannelsString);
            if (StatsData.VoiceChannelString != "")
                embed.AddField("Minutes spent in voice channels (users):", StatsData.VoiceChannelString);

            return embed;
        }

        [Command("stats")]
        public async Task Task1()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                DateTime LastWeekDate = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 7) + (int)DayOfWeek.Monday);
                var Data = await GetStatsFromRange(LastWeekDate.ToString(), LastWeekDate.ToString());
                if (Data.MessagesString == "" && Data.VoiceChannelString == "" && Data.ChannelsString == "")
                    await ReplyAsync("No data to report from last week");
                else
                    await ReplyAsync(embed: CreateEmbed(Data, LastWeekDate.ToShortDateString(), LastWeekDate.ToShortDateString()).Build());
            }
        }

        [Command("statsnow")]
        public async Task Task2()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                DateTime ThisWeekDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                var Data = await GetStatsFromRange(ThisWeekDate.ToString(), ThisWeekDate.ToString());
                if (Data.MessagesString == "" && Data.VoiceChannelString == "" && Data.ChannelsString == "")
                    await ReplyAsync("No data to report from this week");
                else
                    await ReplyAsync(embed: CreateEmbed(Data, ThisWeekDate.ToShortDateString(), ThisWeekDate.ToShortDateString()).Build());
            }
        }

        [Command("statsall")]
        public async Task Task3()
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                var embed = await GetStatsFromRange("", "");
                var Data = await GetStatsFromRange("", "");
                if (Data.MessagesString == "" && Data.VoiceChannelString == "" && Data.ChannelsString == "")
                    await ReplyAsync("No data to report");
                else
                    await ReplyAsync(embed: CreateEmbed(Data, "", "").Build());
            }
        }
    }
}
