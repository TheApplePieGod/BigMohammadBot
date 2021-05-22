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
    public class Emotes : ModuleBase<SocketCommandContext>
    {
        [Command("emotes")]
        public async Task Task1(string Operation = "", string Name = "", string Link = "")
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            Database.DatabaseContext dbContext = new Database.DatabaseContext();
            int CallingUserId = await Globals.GetDbUserId(Context.Message.Author);

            Name = Name.Trim().ToLower();
            Link = Link.Trim();
            Operation = Operation.ToLower();
            if (Operation == "add")
            {
                if (!GuildUser.GuildPermissions.ManageEmojis && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                    throw new Exception("You do not have permission to run that command");

                if (Name == "")
                    throw new Exception("Name cannot be empty");
                if (Link == "")
                    throw new Exception("Link cannot be empty");
                if (!Name.All(char.IsLetterOrDigit))
                    throw new Exception("Name can only contain alphanumeric characters");
                if (Name.Length > 25)
                    throw new Exception("Name must be between 1 and 25 characters");
                if (Link.Length > 300)
                    throw new Exception("Link cannot exceed 300 characters");
                if (Name == "check")
                    throw new Exception("Cannot make an emote with this name");

                var ExistingEmote = await dbContext.Emotes.ToAsyncEnumerable().Where(c => c.Name == Name).FirstOrDefaultAsync();
                if (ExistingEmote != null)
                    await ReplyAsync("An emote with this name already exists");
                else
                {
                    try
                    {
                        Database.Emote NewRow = new Database.Emote();
                        NewRow.Name = Name;
                        NewRow.Link = Link;
                        NewRow.Created = DateTime.Now;
                        dbContext.Emotes.Add(NewRow);
                        await dbContext.SaveChangesAsync();
                        await ReplyAsync("Successfully created emote $" + Name + "$");
                        Globals.LogActivity(10, Name, "", true, CallingUserId);
                    }
                    catch (Exception e)
                    {
                        Globals.LogActivity(10, Name, e.Message, false, CallingUserId);
                        throw new Exception("Operation failed: " + e.Message);
                    }
                }
                        
            }
            else if (Operation == "remove")
            {
                if (!GuildUser.GuildPermissions.ManageEmojis && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                    throw new Exception("You do not have permission to run that command");

                if (Name == "")
                    throw new Exception("Name cannot be empty");

                var ExistingEmote = await dbContext.Emotes.ToAsyncEnumerable().Where(c => c.Name == Name).FirstOrDefaultAsync();
                if (ExistingEmote == null)
                    await ReplyAsync("The emote $" + Name + "$ does not exist");
                else
                {
                    try
                    {
                        dbContext.Emotes.Remove(ExistingEmote);
                        await dbContext.SaveChangesAsync();
                        await ReplyAsync("Successfully removed emote $" + Name + "$");
                        Globals.LogActivity(11, Name, "", true, CallingUserId);
                    }
                    catch (Exception e)
                    {
                        Globals.LogActivity(11, Name, e.Message, false, CallingUserId);
                        throw new Exception("Operation failed: " + e.Message);
                    }
                }         
            }
            else if (Operation == "list")
            {
                string ListString = "";
                var AllEmotes = await dbContext.Emotes.ToAsyncEnumerable().ToListAsync();

                if (AllEmotes.Count == 0)
                {
                    await ReplyAsync("No emotes have been created yet");
                    return;
                }

                foreach (Database.Emote Item in AllEmotes)
                {
                    ListString += Item.Name + '\n';
                }

                var embed = new EmbedBuilder
                {
                    Title = "All Emotes",
                    Description = "Prefixed and suffixed by '$'"
                };
                embed.AddField("List", ListString);

                await ReplyAsync(embed: embed.Build());
            }
            else
                throw new Exception("Must provide a valid operation (add, remove, list)");
        }
    }
}
