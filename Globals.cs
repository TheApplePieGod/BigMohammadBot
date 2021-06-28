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

// REFACTOR
namespace BigMohammadBot
{
    public static class Globals
    {
        public static readonly ulong BeefBossId = 129692115495157760;
        public static readonly List<ulong> AdminUserIds = new List<ulong> { BeefBossId };

#if (DEBUG)
        public static readonly char CommandPrefix = '?';
#else
        public static readonly char CommandPrefix = '$';
#endif

        public static readonly List<string> LoggingIgnoredCommands = new List<string> { CommandPrefix + "greeting" };

        public static async void LogActivity(ulong ClientIdentifier, int ActivityType, string Information, string ResultText, bool Success, int CallingUserId = 0)
        {
            foreach (string Ignored in LoggingIgnoredCommands)
            {
                if (Information.Contains(Ignored))
                    return;
            }

            Database.DatabaseContext dbContext = await DbHelper.GetDbContext(ClientIdentifier);

            Database.ActivityLog Entry = new Database.ActivityLog();
            Entry.TypeId = ActivityType;
            Entry.Information = Information.Substring(0, Math.Min(Information.Length, 100));
            Entry.ResultText = ResultText.Substring(0, Math.Min(ResultText.Length, 200));
            Entry.Success = Success;
            Entry.CallTime = DateTime.Now;
            Entry.CalledByUserId = CallingUserId;
            dbContext.ActivityLogs.Add(Entry);

            await dbContext.SaveChangesAsync();
        }

        public static async Task<int> GetDbUserId(ulong ClientIdentifier, IUser User)
        {
            Database.DatabaseContext dbContext = await DbHelper.GetDbContext(ClientIdentifier);
            return await GetDbUserId(dbContext, ClientIdentifier, User);
        }

        public static async Task<int> GetDbUserId(Database.DatabaseContext dbContext, ulong ClientIdentifier, IUser User)
        {
            var DbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.DiscordUserId.ToInt64() == User.Id).FirstOrDefaultAsync();
            if (DbUser == null)
            {
                Database.User NewUser = new Database.User();
                NewUser.DiscordUserId = User.Id.ToByteArray();
                NewUser.DiscordUserName = User.Username;
                NewUser.LastActive = DateTime.Now;
                dbContext.Users.Add(NewUser);

                await dbContext.SaveChangesAsync();
                return NewUser.Id;
            }
            else
            {
                DbUser.LastActive = DateTime.Now;
                DbUser.DiscordUserName = User.Username;
                await dbContext.SaveChangesAsync();
                return DbUser.Id;
            }
        }

        public static async Task<int> GetDbChannelId(SocketGuildChannel Channel, bool SkipUpdate = false)
        {
            var dbContext = await DbHelper.GetDbContext(Channel.Guild.Id);

            var DbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(u => u.DiscordChannelId.ToInt64() == Channel.Id).FirstOrDefaultAsync();
            if (DbChannel == null)
            {
                Database.Channel NewRow = new Database.Channel();
                NewRow.DiscordChannelId = Channel.Id.ToByteArray();
                NewRow.DiscordChannelName = Channel.Name;
                NewRow.Type = (short)(Channel.GetType().ToString() == "Discord.WebSocket.SocketTextChannel" ? 2 : 1);
                NewRow.LastActive = DateTime.Now;
                NewRow.Deleted = false;
                dbContext.Channels.Add(NewRow);

                await dbContext.SaveChangesAsync();
                return NewRow.Id;
            }
            else
            {
                if (!SkipUpdate)
                {
                    DbChannel.LastActive = DateTime.Now;
                    DbChannel.DiscordChannelName = Channel.Name;
                    await dbContext.SaveChangesAsync();
                }
                return DbChannel.Id;
            }
        }

        public static async Task<int> GetDbChannelId(ulong ClientIdentifier, ulong ChannelId, string ChannelName, int Type, bool SkipUpdate = false)
        {
            Database.DatabaseContext dbContext = await DbHelper.GetDbContext(ClientIdentifier);

            var DbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(u => u.DiscordChannelId.ToInt64() == ChannelId).FirstOrDefaultAsync();
            if (DbChannel == null)
            {
                Database.Channel NewRow = new Database.Channel();
                NewRow.DiscordChannelId = ChannelId.ToByteArray();
                NewRow.DiscordChannelName = ChannelName;
                NewRow.Type = (short)Type;
                NewRow.LastActive = DateTime.Now;
                NewRow.Deleted = false;
                dbContext.Channels.Add(NewRow);

                await dbContext.SaveChangesAsync();
                return NewRow.Id;
            }
            else
            {
                if (!SkipUpdate)
                {
                    DbChannel.LastActive = DateTime.Now;
                    DbChannel.DiscordChannelName = ChannelName;
                    await dbContext.SaveChangesAsync();
                }
                return DbChannel.Id;
            }
        }

        public static async void AwardChainKeeper(SocketTextChannel ResponseChannel, int Iteration, int BreakerUserId, SocketGuild Guild, DiscordSocketClient Client) //todo: log
        {
            var dbContext = await DbHelper.GetDbContext(Guild.Id);
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();
            var MessageCounts = await dbContext.HelloMessageCountModel.FromSqlRaw(@"select * from udf_GetHelloMessageCount(@iteration, @alliterations, @userid, @allusers) order by NumMessages desc",
                    new SqlParameter("@iteration", Iteration),
                    new SqlParameter("@alliterations", false),
                    new SqlParameter("@userid", 1),
                    new SqlParameter("@allusers", true)
                ).ToListAsync();
            if (MessageCounts.Count >= 1)
            {
                int AwardingId = MessageCounts[0].UserId;
                if (MessageCounts[0].UserId == BreakerUserId)
                    if (MessageCounts.Count >= 2)
                        AwardingId = MessageCounts[1].UserId;
                    else
                        AwardingId = 0;

                if (AwardingId != 0)
                {
                    bool RoleEnabled = AppState.ChainKeeperRoleId != null && AppState.ChainKeeperRoleId.Length > 0;

                    var RemovingdbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == AppState.KeeperUserId).FirstOrDefaultAsync();
                    if (RemovingdbUser != null)
                    {
                        if (RoleEnabled)
                        {
                            var KeeperRole = Guild.GetRole(AppState.ChainKeeperRoleId.ToInt64());
                            var User = await Client.Rest.GetGuildUserAsync(Guild.Id, RemovingdbUser.DiscordUserId.ToInt64());
                            await User.RemoveRoleAsync(KeeperRole);
                            AppState.KeeperUserId = 0;
                        }
                    }

                    var dbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == AwardingId).FirstOrDefaultAsync();
                    if (dbUser != null)
                    {
                        if (RoleEnabled)
                        {
                            var KeeperRole = Guild.GetRole(AppState.ChainKeeperRoleId.ToInt64());
                            var User = await Client.Rest.GetGuildUserAsync(Guild.Id, dbUser.DiscordUserId.ToInt64());
                            await User.AddRoleAsync(KeeperRole);
                            await ResponseChannel.SendMessageAsync("<@!" + dbUser.DiscordUserId.ToInt64() + "> has been awarded <@&" + AppState.ChainKeeperRoleId.ToInt64() + ">");
                        }
                        else
                            await ResponseChannel.SendMessageAsync("<@!" + dbUser.DiscordUserId.ToInt64() + "> has been awarded [Keeper of the Chain]");

                        AppState.KeeperUserId = AwardingId;
                        dbUser.AmountKeeper++;
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public static async void SetSuspendedUser(SocketTextChannel ResponseChannel, int NewUserId, SocketGuild Guild, DiscordSocketClient Client) //todo: log
        {
            var dbContext = await DbHelper.GetDbContext(Guild.Id);
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

            if (AppState.SuspendedUserId == NewUserId)
                return;

            bool RoleEnabled = AppState.SuspendedRoleId != null && AppState.SuspendedRoleId.Length > 0;
            if (RoleEnabled)
            {
                var SuspendedRole = Guild.GetRole(AppState.SuspendedRoleId.ToInt64());
                if (AppState.SuspendedUserId != 0)
                {
                    var RemovingdbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == AppState.SuspendedUserId).FirstOrDefaultAsync();
                    if (RemovingdbUser != null)
                    {
                        var User = await Client.Rest.GetGuildUserAsync(Guild.Id, RemovingdbUser.DiscordUserId.ToInt64());
                        await User.RemoveRoleAsync(SuspendedRole);
                        AppState.SuspendedUserId = 0;
                    }
                }

                if (NewUserId != 0)
                {
                    var dbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == NewUserId).FirstOrDefaultAsync();
                    if (dbUser != null)
                    {
                        var User = await Client.Rest.GetGuildUserAsync(Guild.Id, dbUser.DiscordUserId.ToInt64());
                        await User.AddRoleAsync(SuspendedRole);

                        await ResponseChannel.SendMessageAsync("<@!" + dbUser.DiscordUserId.ToInt64() + "> has been suspended from the new chain.");

                        AppState.SuspendedUserId = NewUserId;
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
