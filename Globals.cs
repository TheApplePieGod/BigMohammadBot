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


namespace BigMohammadBot
{
    public static class Globals
    {
        public static readonly ulong BeefBossId = 129692115495157760;
        public static readonly List<ulong> AdminUserIds = new List<ulong>{ BeefBossId };
        
#if (DEBUG)
        public static readonly ulong ChainBreakerRoleId = 755873753464045661;
        public static readonly ulong ChainKeeperRoleId = 757792415179472896;
        public static readonly ulong SuppressTextRoleId = 385580495498641409;
        public static readonly ulong SuspendedRoleId = 783933452733906954;
        public static readonly ulong GeneralChannelId = 364208021171339267;
        public static readonly ulong VotingChannelId = 799741564301737995;
        public static readonly ulong DefaultCategoryId = 364208021171339266;
        public static readonly ulong HelloCategoryId = 783933747711180800;
        public static readonly ulong MohammadServerId = 364208021171339265;
#else
        public static readonly ulong ChainBreakerRoleId = 755845913460867132;
        public static readonly ulong ChainKeeperRoleId = 761208468320550952;
        public static readonly ulong SuppressTextRoleId = 757805274206568558;
        public static readonly ulong SuspendedRoleId = 783932971480252416;
        public static readonly ulong GeneralChannelId = 619209478973292547;
        public static readonly ulong VotingChannelId = 799741511448657940;
        public static readonly ulong DefaultCategoryId = 619209478973292546;
        public static readonly ulong HelloCategoryId = 785595959857774633;
        public static readonly ulong MohammadServerId = 619209478973292545;
#endif

        //public static readonly string HelloChannelTopic = "This is where we chain hi. NO EMOJIS. NO IMAGES. NO GIFS. ABSOLUTELY NO ATTACHMENTS. ONE MESSAGE AT A TIME. IF YOU GO TWICE IN A ROW YOU LOSE. NO REPEATING GREETINGS. Editing messages is NOT allowed. If nobody messages for 12 hours the channel is deleted and we lose. If you break these rules the channel is deleted and you get the <@&" + ChainBreakerRoleId + "> role. This includes deleting a message and sending another. All languages are welcome. If you can justify it as a greeting it goes (discussions are not allowed).";

        public static async void LogActivity(int ActivityType, string Information, string ResultText, bool Success, int CallingUserId = 0)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            Database.ActivityLog Entry = new Database.ActivityLog();
            Entry.TypeId = ActivityType;
            Entry.Information = Information.Substring(0, Math.Min(Information.Length, 100));
            Entry.ResultText = ResultText.Substring(0, Math.Min(ResultText.Length, 200));
            Entry.Success = Success;
            Entry.CallTime = DateTime.Now;
            Entry.CalledByUserId = CallingUserId;
            dbContext.ActivityLog.Add(Entry);

            await dbContext.SaveChangesAsync();
        }

        public static async Task<int> GetDbUserId(IUser User)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var DbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.DiscordUserId.ToInt64() == User.Id).FirstOrDefaultAsync();
            if (DbUser == null)
            {
                Database.Users NewUser = new Database.Users();
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
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var DbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(u => u.DiscordChannelId.ToInt64() == Channel.Id).FirstOrDefaultAsync();
            if (DbChannel == null)
            {
                Database.Channels NewRow = new Database.Channels();
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

        public static async Task<int> GetDbChannelId(ulong ChannelId, string ChannelName, int Type, bool SkipUpdate = false)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();

            var DbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(u => u.DiscordChannelId.ToInt64() == ChannelId).FirstOrDefaultAsync();
            if (DbChannel == null)
            {
                Database.Channels NewRow = new Database.Channels();
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

        public static async void AwardChainKeeper(int Iteration, int BreakerUserId, SocketGuild Guild, DiscordSocketClient Client) //todo: log
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();
            var AppState = await dbContext.AppState.AsAsyncEnumerable().FirstOrDefaultAsync();
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
                    var KeeperRole = Guild.GetRole(ChainKeeperRoleId);
                    var RemovingdbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == AppState.KeeperUserId).FirstOrDefaultAsync();
                    if (RemovingdbUser != null)
                    {
                        var User = await Client.Rest.GetGuildUserAsync(Guild.Id, RemovingdbUser.DiscordUserId.ToInt64());
                        await User.RemoveRoleAsync(KeeperRole);
                        AppState.KeeperUserId = 0;
                    }

                    var dbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == AwardingId).FirstOrDefaultAsync();
                    if (dbUser != null)
                    {
                        var User = await Client.Rest.GetGuildUserAsync(Guild.Id, dbUser.DiscordUserId.ToInt64());
                        await User.AddRoleAsync(KeeperRole);

                        var GeneralChannel = Guild.GetChannel(GeneralChannelId) as SocketTextChannel;
                        await GeneralChannel.SendMessageAsync("<@!" + dbUser.DiscordUserId.ToInt64() + "> has been awarded <@&" + ChainKeeperRoleId + ">");

                        AppState.KeeperUserId = AwardingId;
                        dbUser.AmountKeeper++;
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public static async void SetSuspendedUser(int NewUserId, SocketGuild Guild, DiscordSocketClient Client) //todo: log
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();
            var AppState = await dbContext.AppState.AsAsyncEnumerable().FirstOrDefaultAsync();

            if (AppState.SuspendedUserId == NewUserId)
                return;

            var SuspendedRole = Guild.GetRole(SuspendedRoleId);
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

                    var GeneralChannel = Guild.GetChannel(GeneralChannelId) as SocketTextChannel;
                    await GeneralChannel.SendMessageAsync("<@!" + dbUser.DiscordUserId.ToInt64() + "> has been suspended from the new chain.");

                    AppState.SuspendedUserId = NewUserId;
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
