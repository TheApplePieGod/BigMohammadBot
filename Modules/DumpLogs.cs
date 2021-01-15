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
    public class DumpLogs : ModuleBase<SocketCommandContext>
    {
        
        [Command("dumplogs")]
        public async Task Task1(int count = 10)
        {
            if (Globals.AdminUserIds.Contains(Context.Message.Author.Id) && count < 50) // admin command
            {
                Database.DatabaseContext dbContext = new Database.DatabaseContext();

                var embed = new EmbedBuilder
                {
                    Title = "Logs Output (last 10)",
                    Description = ""
                };

                var Last20Logs = await dbContext.LastLogsModel.FromSqlRaw(@"select * from udf_GetLastLogs(@numlogs)",
                    new SqlParameter("@numlogs", count)
                ).ToListAsync();


                foreach (LastLogs Log in Last20Logs)
                {
                    string LogString = "";
                    string Success = (Log.Success ? " successfully " : " unsuccessfully ");
                    string User = (Log.CalledByUserName != null ? (" by " + Log.CalledByUserName) : "");
                    string Information = (Log.Information != "" ? ("with information \"" + Log.Information + "\"") : "");
                    string Result = (Log.ResultText != "" ? (" with result \"" + Log.ResultText + "\"") : "");

                    if (Log.TypeName == "Command")
                        LogString = Log.CalledByUserName + Success + "called \"" + Log.Information + "\"" + Result;
                    else if (Log.TypeName == "Error")
                        LogString = "Error occured during " + Log.Information + " because " + Log.ResultText;
                    else
                        LogString = Log.TypeName + " occured" + User + Success + Information + Result; // generic

                    embed.AddField(Log.CallTime.ToString() + ":", LogString);
                }

                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}
