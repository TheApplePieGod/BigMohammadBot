//#define DEBUG

using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Data.SqlClient;
using Discord;
using System.Reflection;
using DbUp;
using System.IO;

namespace BigMohammadBot
{
    public static class DbHelper
    {
        //public static Dictionary<ulong, Database.DatabaseContext> SavedContexts = new Dictionary<ulong, Database.DatabaseContext>();
        public static Dictionary<ulong, string> ContextLoaded = new Dictionary<ulong, string>();

        public static async Task<Database.DatabaseContext> GetDbContext(ulong ClientIdentifier)
        {
            if (ContextLoaded.ContainsKey(ClientIdentifier))
            {
                return new Database.DatabaseContext(ContextLoaded[ClientIdentifier]);
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                string BaseConnectionString = "Server=.\\SQLEXPRESS;Database=BigMohammadBot;Trusted_Connection=True;";
#if (!DEBUG)
                var DbStringFile = "BigMohammadBot.Data.DbString.txt";
                using (Stream stream = assembly.GetManifestResourceStream(DbStringFile))
                using (StreamReader reader = new StreamReader(stream))
                    BaseConnectionString = reader.ReadToEnd().Trim();
#endif

                string ConnectionString = BaseConnectionString.Replace("BigMohammadBot", "BigMohammadBot_" + ClientIdentifier);
                var NewContext = new Database.DatabaseContext(ConnectionString);
                //await NewContext.Database.EnsureCreatedAsync();

                // DBUP
                EnsureDatabase.For.SqlDatabase(ConnectionString);
                Task.Delay(1000).Wait();

                var upgradeEngine = DeployChanges.To
                    .SqlDatabase(ConnectionString)
                    .WithScriptsAndCodeEmbeddedInAssembly(assembly, (string s) => s.StartsWith("BigMohammadBot.Migrations.Script"))
                    .WithTransactionPerScript()
                    .WithExecutionTimeout(TimeSpan.FromMilliseconds(30000))
                    .LogToConsole()
                    .Build();

                if (upgradeEngine.IsUpgradeRequired())
                {
                    var result = upgradeEngine.PerformUpgrade();
                    if (!result.Successful)
                        throw result.Error;
                }

                var AppState = await NewContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();
                if (AppState == null) // insert default value
                {
                    AppState = new Database.AppState
                    {
                        LastHelloUserId = 0,
                        LastHelloMessage = null,
                        HelloDeleted = true,
                        HelloChannelId = 0,
                        StatisticsPeriodStart = DateTime.Now,
                        HelloIteration = 0,
                        AutoCreateNewHello = true,
                        HelloTimerNotified = false,
                        KeeperUserId = 0,
                        SuspendedUserId = 0,
                        HelloTopic = "This is where we chain hi. NO EMOJIS. NO IMAGES. NO GIFS. ABSOLUTELY NO ATTACHMENTS. ONE MESSAGE AT A TIME. IF YOU GO TWICE IN A ROW YOU LOSE. NO REPEATING GREETINGS. Editing messages is NOT allowed. If nobody messages for 12 hours the channel is deleted and we lose. If you break these rules the channel is deleted and you get the ChainBreaker role. This includes deleting a message and sending another. All languages are welcome. If you can justify it as a greeting it goes (discussions are not allowed).",
                        JoinMuteMinutes = 1440,
                        ResponseChannelId = null,
                        HelloCategoryId = null,
                        ChainBreakerRoleId = null,
                        ChainKeeperRoleId = null,
                        SuppressedRoleId = null,
                        SuspendedRoleId = null,
                        EnableHelloChain = true,
                        EnableStatisticsTracking = true,
                        EnableMeCommand = true,
                        EnableEmotes = true
                    };
                    NewContext.AppStates.Add(AppState);
                }

                // Initialize
                try
                {
                    AppState.LastHelloUserId = 0;  // reset just for safety reasons

                    if (AppState.EnableHelloChain && !AppState.HelloDeleted.Value && AppState.HelloChannelId != 0)
                    {
                        var Channel = await NewContext.Channels.ToAsyncEnumerable().Where(c => c.Id == AppState.HelloChannelId).FirstOrDefaultAsync();
                        if (Channel != null)
                        {
                            var HelloChannel = CommandHandler._client.GetChannel(Channel.DiscordChannelId.ToInt64()) as SocketTextChannel;
                            if (HelloChannel != null)
                            {
                                var LastMessage = await HelloChannel.GetMessagesAsync(1).Flatten().FirstOrDefaultAsync();
                                if (LastMessage != null)
                                {
                                    AppState.LastHelloUserId = await Globals.GetDbUserId(NewContext, ClientIdentifier, LastMessage.Author);
                                    AppState.LastHelloMessage = LastMessage.Timestamp.DateTime.ToLocalTime();
                                }
                            }
                        }
                    }

                    await NewContext.SaveChangesAsync();
                }
                catch (Exception e) { Globals.LogActivity(ClientIdentifier, 1, "Initialize", e.Message, false); throw e; }

                ContextLoaded[ClientIdentifier] = ConnectionString;
                return NewContext;
            }
        }
    }
}
