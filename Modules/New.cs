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
    public class New : ModuleBase<SocketCommandContext>
    {
        [Command("new")]
        public async Task Task1()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var HelpFileName = "BigMohammadBot.Data.New.txt";
            string Footer = "DM <@!755524359753564311> for help, bugs, etc.";

            var embed = new EmbedBuilder
            {
                Title = "What's New",
                Description = ""
            };

            using (Stream stream = assembly.GetManifestResourceStream(HelpFileName))
            using (StreamReader reader = new StreamReader(stream))
                embed.AddField("Versions", reader.ReadToEnd());

            embed.AddField("Other", Footer);

            await ReplyAsync(embed: embed.Build());
        }
    }
}
