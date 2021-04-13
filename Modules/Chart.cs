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
    public class Chart : ModuleBase<SocketCommandContext>
    {
        public EmbedBuilder CreateEmbed(string URL, int NumWeeks)
        {
            var embed = new EmbedBuilder
            {
                Title = "Activity History",
                Description = "Past " + NumWeeks + " Weeks",
                ImageUrl = URL
            };
            return embed;
        }

        [Command("reportchart")]
        public async Task Task1(int NumWeeks, params string[] MentionedUserStrings)
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                if (MentionedUserStrings.Length == 0)
                    throw new ArgumentException("Must mention at least one user");
                if (MentionedUserStrings.Length > 5)
                    throw new ArgumentException("Cannot chart more than five users at a time");

                // 0 weeks = all time
                if (NumWeeks == 0)
                {
                    DateTime ThisWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                    DateTime DataStart = DateTime.Parse("2020-11-30 00:00:00.000");
                    NumWeeks = ((ThisWeek - DataStart).Days / 7) + 1;
                }

                DateTime StartingWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday).AddDays(-7 * (NumWeeks - 1));
                string Labels = "[";
                string DataSets = "";
                Dictionary<string, List<decimal>> DataValues = new Dictionary<string, List<decimal>>();

                List<ulong> MentionedIds = new List<ulong>();
                foreach (string elem in MentionedUserStrings)
                {
                    string FormattedId = new string(elem.Where(char.IsNumber).ToArray());
                    MentionedIds.Add(ulong.Parse(FormattedId));
                }

                for (int i = 0; i < NumWeeks; i++)
                {
                    Labels += "'" + StartingWeek.ToShortDateString() + "'";
                    if (i != NumWeeks - 1)
                        Labels += ", ";

                    ReportReturn ReportData = await Report.ReportRange(StartingWeek.ToString(), StartingWeek.ToString());

                    List<string> AllKeys = new List<string>(DataValues.Keys);
                    foreach (UserPercent stat in ReportData.PercentageList)
                    {
                        if (!MentionedIds.Contains(stat.DiscordId))
                            continue;

                        if (DataValues.ContainsKey(stat.UserName))
                        {
                            DataValues[stat.UserName].Add(stat.Percent);
                            AllKeys.Remove(stat.UserName);
                        }
                        else // new person so add values to match with the amount of weeks so far
                        {
                            DataValues[stat.UserName] = new List<decimal>();
                            for (int j = 0; j < i; j++)
                            {
                                DataValues[stat.UserName].Add(0);
                            }
                            DataValues[stat.UserName].Add(stat.Percent);
                        }
                    }

                    // loop over any remaining keys and add zero so the number of values remains constant
                    foreach (string Key in AllKeys)
                    {
                        DataValues[Key].Add(0);
                    }

                    StartingWeek = StartingWeek.AddDays(7);
                }
                Labels += ']';

                // build the json data
                for (int i = 0; i < DataValues.Keys.Count; i++)
                {
                    string Key = DataValues.Keys.ElementAt(i);
                    DataSets += "{label:'";
                    DataSets += Key.Replace("/", "").Replace("\\", "");
                    DataSets += "',data:[";
                    for (int j = 0; j < DataValues[Key].Count; j++)
                    {
                        DataSets += DataValues[Key][j].ToString("N1");
                        if (j != DataValues[Key].Count - 1)
                            DataSets += ',';
                    }
                    DataSets += "],fill:false}";
                    if (i != DataValues.Keys.Count - 1)
                        DataSets += ',';
                }
                
                if (DataValues.Keys.Count == 0)
                    await ReplyAsync("No data to report for this range");
                else
                {
                    var ChartJson = $@"{{ type: 'line', data: {{ labels: {Labels}, datasets: [{DataSets}] }} }}";

                    var URL = "https://quickchart.io/chart?backgroundColor=white&c=" + Uri.EscapeDataString(ChartJson);
                    await ReplyAsync(embed: CreateEmbed(URL, NumWeeks).Build());
                }
            }
        }
    }
}
