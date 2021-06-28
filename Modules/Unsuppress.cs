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
    public class Unsuppress : ModuleBase<SocketCommandContext>
    {
        [Command("unmute")]
        public async Task Task1(string MentionedUser)
        {
            string FormattedId = new string(MentionedUser.Where(char.IsNumber).ToArray());
            var User = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, ulong.Parse(FormattedId));

            if (User == null)
                throw new Exception("User not found");

            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
                var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

                if (AppState.SuppressedRoleId != null && AppState.SuppressedRoleId.Length > 0)
                {
                    int UserId = await Globals.GetDbUserId(Context.Guild.Id, User);
                    var SuppressedUserRow = await dbContext.SupressedUsers.ToAsyncEnumerable().Where(u => u.UserId == UserId).FirstOrDefaultAsync();

                    if (SuppressedUserRow != null)
                        dbContext.SupressedUsers.Remove(SuppressedUserRow);

                    var SuppressRole = Context.Guild.GetRole(AppState.SuppressedRoleId.ToInt64());
                    await User.RemoveRoleAsync(SuppressRole);

                    await dbContext.SaveChangesAsync();

                    //await Context.Message.DeleteAsync();
                    await ReplyAsync("<@!" + User.Id + "> has been unmuted.");

                    int CallingUserId = await Globals.GetDbUserId(Context.Guild.Id, Context.Message.Author);
                    Globals.LogActivity(Context.Guild.Id, 2, "", User.Username + " has been unmuted.", true, CallingUserId);
                }
                else
                    await ReplyAsync("This feature has not been set up yet");
            }
        }
    }
}
