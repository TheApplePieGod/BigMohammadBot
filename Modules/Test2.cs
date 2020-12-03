using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TheKekBotREAL.Modules
{
    public class Test2 : ModuleBase<SocketCommandContext>
    {
        
        [Command("dsa")]
        public async Task Task1([Remainder] int NumDelete = 0) //place parameters for command in these ()
        {
            SocketUser dduser = Context.User;

            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Bots");

            await (dduser as IGuildUser).AddRoleAsync(role);

        }
    }
}
