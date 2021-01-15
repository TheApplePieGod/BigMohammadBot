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
    public class Suppress : ModuleBase<SocketCommandContext>
    {
        [Command("mute")]
        public async Task Task1(string MentionedUser, int Minutes)
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
                if (Minutes > 0)
                {
                    var dbContext = new Database.DatabaseContext();
                    int UserId = await Globals.GetDbUserId(User);
                    var SuppressedUserRow = await dbContext.SupressedUsers.ToAsyncEnumerable().Where(u => u.UserId == UserId).FirstOrDefault();

                    if (SuppressedUserRow == null)
                    {
                        Database.SupressedUsers NewRow = new Database.SupressedUsers();
                        NewRow.UserId = UserId;
                        NewRow.TimeStarted = DateTime.Now;
                        NewRow.MaxTimeSeconds = Minutes * 60;
                        dbContext.SupressedUsers.Add(NewRow);
                    }
                    else
                    {
                        SuppressedUserRow.TimeStarted = DateTime.Now;
                        SuppressedUserRow.MaxTimeSeconds = Minutes * 60;
                    }

                    var SuppressRole = Context.Guild.GetRole(Globals.SuppressTextRoleId);
                    await User.AddRoleAsync(SuppressRole);

                    await dbContext.SaveChangesAsync();

                    //await Context.Message.DeleteAsync();
                    await ReplyAsync("<@!" + User.Id + "> has been muted for " + Minutes + " minutes.");

                    int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);
                    Globals.LogActivity(3, "", User.Username + " has been muted for " + Minutes + " minutes.", true, CallingUserId);
                }
                else
                {
                    //await Context.Message.DeleteAsync();
                    throw new Exception("Cannot mute for 0 minutes");
                }

            }
        }
    }
}
