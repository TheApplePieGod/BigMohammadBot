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

        public async Task StartAsync()
        {
            //Globals.dbContext = new Database.DatabaseContext();
            //var d = Globals.dbContext.ChangeTracker;

            // DBUP
            string dbConnectionString = "";
            var assembly = Assembly.GetExecutingAssembly();
            var DbStringFile = "BigMohammadBot.Data.DbString.txt";

#if (DEBUG)
            dbConnectionString = "Server=.\\SQLEXPRESS;Database=BigMohammadBot;Trusted_Connection=True;";
#else
            using (Stream stream = assembly.GetManifestResourceStream(DbStringFile))
            using (StreamReader reader = new StreamReader(stream))
                dbConnectionString = reader.ReadToEnd().Trim();
#endif

			EnsureDatabase.For.SqlDatabase(dbConnectionString);
			Task.Delay(1000).Wait();

			var upgradeEngine = DeployChanges.To
				.SqlDatabase(dbConnectionString)
				.WithScriptsAndCodeEmbeddedInAssembly(assembly, (string s) => s.StartsWith("BigMohammadBot.Migrations.Script"))
				.WithTransactionPerScript()
				.WithExecutionTimeout(TimeSpan.FromMilliseconds(30000))
				.LogToConsole()
				.Build();

			if (upgradeEngine.IsUpgradeRequired())
			{
				var result = upgradeEngine.PerformUpgrade();

				if (!result.Successful)
				{
					throw result.Error;
				}
			}

			_client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true
            });
            //_client = new DiscordSocketClient();

            var CredentialFile = "BigMohammadBot.Data.BotCredential.txt";

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