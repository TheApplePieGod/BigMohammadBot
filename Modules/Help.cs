using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using System.IO;

namespace BigMohammadBot.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task Task1()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var HelpFileName = "BigMohammadBot.Data.Help.txt";
            var ChatModeratorHelpFileName = "BigMohammadBot.Data.HelpChatModerator.txt";
            var AdminHelpFileName = "BigMohammadBot.Data.HelpAdmin.txt";
            var StatsHelpFileName = "BigMohammadBot.Data.HelpStats.txt";
            string Footer = "DM <@!755524359753564311> for help, bugs, etc.";

            var embed = new EmbedBuilder
            {
                Title = "Help",
                Description = "Do not include parenthesis in commands"
            };

            using (Stream stream = assembly.GetManifestResourceStream(HelpFileName))
            using (StreamReader reader = new StreamReader(stream))
                embed.AddField("Commands", reader.ReadToEnd());

            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (GuildUser.GuildPermissions.ManageMessages) // chat moderator
            {
                using (Stream stream = assembly.GetManifestResourceStream(ChatModeratorHelpFileName))
                using (StreamReader reader = new StreamReader(stream))
                    embed.AddField("Chat Moderator Commands", reader.ReadToEnd());
            }
            if (GuildUser.GuildPermissions.Administrator || Globals.AdminUserIds.Contains(Context.Message.Author.Id))
            {
                using (Stream stream = assembly.GetManifestResourceStream(AdminHelpFileName))
                using (StreamReader reader = new StreamReader(stream))
                    embed.AddField("Big Mo Admin Commands", reader.ReadToEnd());

                using (Stream stream = assembly.GetManifestResourceStream(StatsHelpFileName))
                using (StreamReader reader = new StreamReader(stream))
                    embed.AddField("Stats Commands (Big Mo Admin)", reader.ReadToEnd());
            }

            embed.AddField("Other", Footer);

            await ReplyAsync(embed: embed.Build());
        }
    }
}
