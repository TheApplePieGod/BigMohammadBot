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
    public class Unsuspend : ModuleBase<SocketCommandContext>
    {
        [Command("unsuspend")]
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
                var dbContext = new Database.DatabaseContext();
                int UserId = await Globals.GetDbUserId(User);
                var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

                if (AppState.SuspendedUserId == UserId)
                    AppState.SuspendedUserId = 0;

                var SuspendRole = Context.Guild.GetRole(Globals.SuspendedRoleId);
                await User.RemoveRoleAsync(SuspendRole);

                await dbContext.SaveChangesAsync();

                await ReplyAsync("<@!" + User.Id + "> has been unsuspended.");

                //todo: log
                //int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);
                //Globals.LogActivity(2, "", User.Username + " has been unmuted.", true, CallingUserId);
            }
        }
    }
}
