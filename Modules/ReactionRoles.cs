using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace BigMohammadBot.Modules
{
    public class ReactionRoles : ModuleBase<SocketCommandContext>
    {
        
        [Command("reactionroles")]
        public async Task Task1(string ChannelId, ulong MessageId, params string[] Parameters)
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                if (Parameters.Length == 0 || Parameters.Length % 2 != 0)
                    throw new Exception("Incorrect number of arguments supplied");

                var FoundMessage = await (Context.Guild.GetChannel(ulong.Parse(ChannelId.Where(char.IsNumber).ToArray())) as SocketTextChannel).GetMessageAsync(MessageId);
                if (FoundMessage == null)
                    throw new Exception("Supplied message could not be located");

                var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);

                var FoundEntry = await dbContext.ReactionRoles.AsAsyncEnumerable().Where(e => e.MessageId == MessageId.ToByteArray()).FirstOrDefaultAsync();
                if (FoundEntry != null)
                    throw new Exception("Reaction roles have already been set up for this message");

                var NewMessage = new Database.ReactionRole
                {
                    Deleted = false,
                    MessageId = MessageId.ToByteArray()
                };
                dbContext.ReactionRoles.Add(NewMessage);
                await dbContext.SaveChangesAsync();

                for (int i = 0; i < Parameters.Length; i += 2)
                {
                    var NewEmote = new Database.ReactionRoleEmote
                    {
                        ReactionRoleId = NewMessage.Id,
                        Emote = Parameters[i],
                        RoleId = ulong.Parse(Parameters[i + 1].Where(char.IsNumber).ToArray()).ToByteArray()
                    };
                    dbContext.ReactionRoleEmotes.Add(NewEmote);
                    Emote ParsedEmote = null;
                    if (Emote.TryParse(Parameters[i], out ParsedEmote))
                        await FoundMessage.AddReactionAsync(ParsedEmote);
                    else
                        await FoundMessage.AddReactionAsync(new Emoji(Parameters[i]));
                }

                await Context.Message.DeleteAsync();
                await dbContext.SaveChangesAsync();
            }
        }
    }
}