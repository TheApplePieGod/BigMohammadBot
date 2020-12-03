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
    public class ChannelBlacklist : ModuleBase<SocketCommandContext>
    {
        [Command("blacklist")]
        public async Task Task1(string Operation, SocketGuildChannel Channel = null)
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");
            else
            {
                Database.DatabaseContext dbContext = new Database.DatabaseContext();
                int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);

                Operation = Operation.ToLower();
                if (Operation == "add")
                {
                    if (Channel != null)
                    {
                        int dbChannelId = await Globals.GetDbChannelId(Channel);
                        var BlacklistedChannel = await dbContext.ChannelBlacklist.ToAsyncEnumerable().Where(c => c.ChannelId == dbChannelId).FirstOrDefault();
                        if (BlacklistedChannel != null)
                            await ReplyAsync("Channel <#" + Channel.Id + "> already blacklisted");
                        else
                        {
                            try
                            {
                                Database.ChannelBlacklist NewRow = new Database.ChannelBlacklist();
                                NewRow.ChannelId = dbChannelId;
                                dbContext.ChannelBlacklist.Add(NewRow);
                                await dbContext.SaveChangesAsync();
                                await ReplyAsync("Successfully blacklisted channel <#" + Channel.Id + "> from statistics");
                                Globals.LogActivity(8, "", Channel.Name, true, CallingUserId);
                            }
                            catch (Exception e)
                            {
                                Globals.LogActivity(8, Channel.Name, e.Message, false, CallingUserId);
                                throw new Exception("Operation failed: " + e.Message);
                            }
                        }
                    }
                    else
                        throw new Exception("Channel cannot be null");
                }
                else if (Operation == "remove")
                {
                    if (Channel != null)
                    {
                        int dbChannelId = await Globals.GetDbChannelId(Channel);
                        var BlacklistedChannel = await dbContext.ChannelBlacklist.ToAsyncEnumerable().Where(c => c.ChannelId == dbChannelId).FirstOrDefault();
                        if (BlacklistedChannel == null)
                            await ReplyAsync("Channel <#" + Channel.Id + "> is not blacklisted");
                        else
                        {
                            try
                            {
                                dbContext.ChannelBlacklist.Remove(BlacklistedChannel);
                                await dbContext.SaveChangesAsync();
                                await ReplyAsync("Successfully removed channel <#" + Channel.Id + "> from blacklist");
                                Globals.LogActivity(9, "", Channel.Name, true, CallingUserId);
                            }
                            catch (Exception e)
                            {
                                Globals.LogActivity(9, Channel.Name, e.Message, false, CallingUserId);
                                throw new Exception("Operation failed: " + e.Message);
                            }
                        }
                    }
                    else
                        throw new Exception("Channel cannot be null");
                }
                else if (Operation == "list")
                {
                    string ListString = "";
                    var Blacklist = await dbContext.ChannelBlacklist.ToListAsync();
                    var AllChannels = await dbContext.Channels.ToAsyncEnumerable().Where(c => Blacklist.Exists(b => b.ChannelId == c.Id)).ToList();

                    foreach (Database.ChannelBlacklist Item in Blacklist)
                    {
                        var FoundChannel = AllChannels.Find(c => c.Id == Item.ChannelId);
                        if (FoundChannel != null && !FoundChannel.Deleted)
                            ListString += "<#" + FoundChannel.DiscordChannelId.ToInt64() + ">\n";
                    }

                    var embed = new EmbedBuilder
                    {
                        Title = "Channel blacklist",
                        Description = ListString
                    };

                    await ReplyAsync(embed: embed.Build());
                }
                else
                    throw new Exception("Must provide a valid operation (add, remove, list)");

                //await Context.Message.DeleteAsync();
            }
        }
    }
}
