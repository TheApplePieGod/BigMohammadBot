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
                var SuppressedUserRow = await dbContext.SupressedUsers.ToAsyncEnumerable().Where(u => u.UserId == UserId).FirstOrDefault();

                if (SuppressedUserRow != null)
                    dbContext.SupressedUsers.Remove(SuppressedUserRow);

                var SuppressRole = Context.Guild.GetRole(Globals.SuppressTextRoleId);
                await User.RemoveRoleAsync(SuppressRole);

                await dbContext.SaveChangesAsync();

                //await Context.Message.DeleteAsync();
                await ReplyAsync("<@!" + User.Id + "> has been unmuted.");

                int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);
                Globals.LogActivity(2, "", User.Username + " has been unmuted.", true, CallingUserId);
            }
        }
    }
}
