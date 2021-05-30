using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using BigMohammadBot.Database.FunctionModels;
using System.IO;
using System.Reflection;

namespace BigMohammadBot.Modules
{
    public class Fun : ModuleBase<SocketCommandContext>
    {
        private List<string> Greetings = new List<string>();

        public Fun()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var GreetingsFileName = "BigMohammadBot.Data.Words.txt";
            using (Stream stream = assembly.GetManifestResourceStream(GreetingsFileName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string l = "";
                while ((l = reader.ReadLine()) != null) { Greetings.Add(l); }
            }
        }

        [Command("greeting")]
        public async Task Task1(int Amount = 1)
        {
            if (Amount == 0)
                throw new Exception("Amount cannot be zero");
            if (Amount > 10)
                throw new Exception("Amount cannot be more than 10");

            Random ran = new Random();
            string Reply = "";

            for (int i = 0; i < Amount; i++)
            {
                int Index = ran.Next(0, Greetings.Count);
                Reply += "- " + Greetings[Index] + '\n';
            }

            await ReplyAsync(Reply);
        }
    }
}
