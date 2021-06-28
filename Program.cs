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
using DbUp;

namespace BigMohammadBot
{
    public class Program
    {
        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private CommandHandler _handler;

        public Task OnLogAsync(LogMessage msg)
        {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            //Globals.dbContext = new Database.DatabaseContext();
            //var d = Globals.dbContext.ChangeTracker;

			_client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Info
            });
            //_client = new DiscordSocketClient();
            _client.Log += OnLogAsync;

            var CredentialFile = "BigMohammadBot.Data.BotCredential.txt";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(CredentialFile))
            using (StreamReader reader = new StreamReader(stream))
                await _client.LoginAsync(TokenType.Bot, reader.ReadToEnd().Trim());
            await _client.StartAsync();

            _handler = new CommandHandler(_client);

            await Task.Delay(-1);
        }
        
    }
}


// When adding new sections to the player file, make sure to check everything, including modules
// alt is 257573869840367616