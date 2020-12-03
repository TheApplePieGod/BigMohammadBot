using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.Webhook;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using System.Linq;
using System.Text.RegularExpressions;
using Discord.Rest;

namespace BigMohammadBot
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;

        private CommandService _commands;

        private const int GenericUpdateDelay = 10; // seconds
        private System.Threading.Timer HelloChainTimer;
        private System.Threading.Timer GenericUpdateTimer;
        private System.Threading.Timer SparseUpdateTimer;
        private bool IsReady = false;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _commands = new CommandService();

            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            _client.MessageReceived += HandleCommandAsync;

            _client.Ready += Ready;
        }

        public async Task Ready()
        {
            await Initialize();
            IsReady = true;
        }

        public async void DeleteHelloChannel(string Message, ulong HelloChannelId)
        {
            var GeneralChannel = _client.GetChannel(Globals.GeneralChannelId) as SocketTextChannel;
            var HelloChannel = _client.GetChannel(HelloChannelId) as SocketTextChannel;
            if (HelloChannel != null && GeneralChannel != null)
            {
                await HelloChannel.DeleteAsync();
                await GeneralChannel.SendMessageAsync(Message);
            }
        }

        public async void SparseTimedUpdate(object state) // runs every 12 hours
        {
            var dbContext = new Database.DatabaseContext();
            var AllChannels = _client.GetGuild(Globals.MohammadServerId).Channels;
            var AllDbChannels = await dbContext.Channels.ToListAsync();
            foreach (Database.Channels Channel in AllDbChannels)
            {
                if (!Channel.Deleted)
                {
                    var FoundChannel = AllChannels.Where(c => c.Id == Channel.DiscordChannelId.ToInt64()).FirstOrDefault();
                    if (FoundChannel == null)
                        Channel.Deleted = true;
                }
            }
            await dbContext.SaveChangesAsync();
        }

        public async void HelloChainTimerUpdate(object state)
        {
            try
            {
                var dbContext = new Database.DatabaseContext();
                var AppState = await dbContext.AppState.FirstOrDefaultAsync();

                if (!AppState.HelloDeleted.Value && AppState.HelloChannelId != 0)
                {
                    DateTime CurrentTime = DateTime.Now;
                    TimeSpan TimeDifference = (CurrentTime - AppState.LastHelloMessage.Value);

                    if (TimeDifference.TotalHours >= 12)
                    {
                        try
                        {
                            var dbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(c => c.Id == AppState.HelloChannelId).FirstOrDefault();

                            DeleteHelloChannel("A hello has not been chained for more than a day. The channel has been deleted.", dbChannel.DiscordChannelId.ToInt64());

                            dbChannel.Deleted = true;
                            AppState.HelloDeleted = true;
                            AppState.HelloTimerNotified = false;

                            await dbContext.SaveChangesAsync();

                            Globals.AwardChainKeeper(AppState.HelloIteration, 0, _client.GetGuild(Globals.MohammadServerId), _client);

                            Globals.LogActivity(4, "Surpassed 12 hours", "", true);
                        }
                        catch (Exception e) { Globals.LogActivity(4, "Surpassed 12 hours", e.Message, false); }
                    }
                    else if (!AppState.HelloTimerNotified.Value && TimeDifference.TotalHours >= 11)
                    {
                        var GeneralChannel = _client.GetChannel(Globals.GeneralChannelId) as SocketTextChannel;
                        await GeneralChannel.SendMessageAsync("Hello chain will be deleted in an hour or less, make sure to refresh the timer");

                        AppState.HelloTimerNotified = true;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e) { Globals.LogActivity(1, "HelloChainTimerUpdate", e.Message, false); }
        }

        public async void GenericTimedUpdate(object state)
        {
            try
            {
                var dbContext = new Database.DatabaseContext();
                var VoiceChannels = _client.GetGuild(Globals.MohammadServerId).VoiceChannels;
                foreach (SocketVoiceChannel Channel in VoiceChannels)
                {
                    if (Channel.Users.Count > 0)
                    {
                        foreach (SocketGuildUser User in Channel.Users)
                        {
                            if (!User.IsBot && !User.IsSelfMuted && !User.IsSelfDeafened)
                            {
                                DateTime CurrentWeekDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                                int UserId = await Globals.GetDbUserId(User);
                                int ChannelId = await Globals.GetDbChannelId(Channel);
                                var VoiceStatisticsRow = await dbContext.VoiceStatistics.ToAsyncEnumerable().Where(u => u.UserId == UserId && u.ChannelId == ChannelId && u.TimePeriod == CurrentWeekDate).FirstOrDefault();

                                if (VoiceStatisticsRow == null)
                                {
                                    Database.VoiceStatistics NewRow = new Database.VoiceStatistics();
                                    NewRow.UserId = UserId;
                                    NewRow.TimeInChannel = GenericUpdateDelay;
                                    NewRow.ChannelId = ChannelId;
                                    NewRow.LastInChannel = DateTime.Now;
                                    NewRow.TimePeriod = CurrentWeekDate;
                                    dbContext.VoiceStatistics.Add(NewRow);
                                }
                                else
                                {
                                    VoiceStatisticsRow.TimeInChannel = VoiceStatisticsRow.TimeInChannel + GenericUpdateDelay;
                                    VoiceStatisticsRow.LastInChannel = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                var SuppressedUsers = await dbContext.SupressedUsers.ToListAsync();
                if (SuppressedUsers.Count > 0)
                {
                    var SuppressRole = _client.GetGuild(Globals.MohammadServerId).GetRole(Globals.SuppressTextRoleId);
                    foreach (Database.SupressedUsers SuppressedUser in SuppressedUsers)
                    {
                        double SecondsDifference = (DateTime.Now - SuppressedUser.TimeStarted.Value).TotalSeconds;
                        if (SecondsDifference >= SuppressedUser.MaxTimeSeconds)
                        {
                            var User = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == SuppressedUser.UserId).FirstOrDefault(); // should exist at this point
                            try
                            {
                                ulong DiscordId = User.DiscordUserId.ToInt64();
                                var GuildUser = await _client.Rest.GetGuildUserAsync(Globals.MohammadServerId, DiscordId);
                                await GuildUser.RemoveRoleAsync(SuppressRole);
                                await (_client.GetChannel(Globals.GeneralChannelId) as ITextChannel).SendMessageAsync("<@!" + DiscordId + "> has been unmuted");
                                dbContext.SupressedUsers.Remove(SuppressedUser);
                                Globals.LogActivity(2, User.DiscordUserName, "", true);
                            }
                            catch (Exception e) { Globals.LogActivity(2, User.DiscordUserName, e.Message, false); }
                        }
                    }
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception e) { Globals.LogActivity(1, "GenericTimedUpdate", e.Message, false); }
        }

        public async Task Initialize()
        {
            try
            {
                //👀
                Game Activity = new Game("👀 ($help)", ActivityType.Watching);
                await _client.SetActivityAsync(Activity);

                var dbContext = new Database.DatabaseContext();
                var AppState = await dbContext.AppState.FirstOrDefaultAsync();
                AppState.LastHelloUserId = 0;  // reset just for safety reasons

                if (!AppState.HelloDeleted.Value && AppState.HelloChannelId != 0)
                {
                    var Channel = await dbContext.Channels.ToAsyncEnumerable().Where(c => c.Id == AppState.HelloChannelId).FirstOrDefault();
                    if (Channel != null)
                    {
                        var HelloChannel = _client.GetChannel(Channel.DiscordChannelId.ToInt64()) as SocketTextChannel;
                        if (HelloChannel != null)
                        {
                            var LastMessage = await HelloChannel.GetMessagesAsync(1).Flatten().FirstOrDefault();
                            if (LastMessage != null)
                            {
                                AppState.LastHelloUserId = await Globals.GetDbUserId(LastMessage.Author);
                                AppState.LastHelloMessage = LastMessage.Timestamp.DateTime.ToLocalTime();
                            }
                        }
                    }
                }

                await dbContext.SaveChangesAsync();

                HelloChainTimer = new System.Threading.Timer(HelloChainTimerUpdate, null, 0, 1000 * 3);
                GenericUpdateTimer = new System.Threading.Timer(GenericTimedUpdate, null, 0, 1000 * GenericUpdateDelay);
                SparseUpdateTimer = new System.Threading.Timer(SparseTimedUpdate, null, 0, 1000 * 60 * 60 * 12); // 12 hours
            }
            catch (Exception e) { Globals.LogActivity(1, "Initialize", e.Message, false); throw e; }
        }

        public async Task UpdateSentMessages(SocketCommandContext Context)
        {
            try
            {
                DateTime CurrentWeekDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                var dbContext = new Database.DatabaseContext();
                int UserId = await Globals.GetDbUserId(Context.Message.Author);
                int ChannelId = await Globals.GetDbChannelId(Context.Guild.GetChannel(Context.Channel.Id)); // context.channel should always be in a guild here
                var MessageStatisticsRow = await dbContext.MessageStatistics.ToAsyncEnumerable().Where(u => u.UserId == UserId && u.ChannelId == ChannelId && u.TimePeriod == CurrentWeekDate).FirstOrDefault();

                if (MessageStatisticsRow == null)
                {
                    Database.MessageStatistics NewRow = new Database.MessageStatistics();
                    NewRow.UserId = UserId;
                    NewRow.MessagesSent = 1;
                    NewRow.ChannelId = ChannelId;
                    NewRow.LastSent = DateTime.Now;
                    NewRow.TimePeriod = CurrentWeekDate;
                    dbContext.MessageStatistics.Add(NewRow);
                }
                else
                {
                    MessageStatisticsRow.MessagesSent = MessageStatisticsRow.MessagesSent + 1;
                    MessageStatisticsRow.LastSent = DateTime.Now;
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception e) { Globals.LogActivity(1, "UpdateSentMessages", e.Message, false); }
        }

        public async Task UpdateHelloChain(SocketCommandContext Context, Database.AppState State)
        {
            Database.DatabaseContext dbContext = new Database.DatabaseContext();
            try
            {
                if (State.HelloChannelId != 0)
                {
                    var dbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(c => c.Id == State.HelloChannelId).FirstOrDefault(); // todo: change back to id instead of channel reference for performance?
                    if (Context.Message.Channel.Id == dbChannel.DiscordChannelId.ToInt64())
                    {
                        int UserId = await Globals.GetDbUserId(Context.Message.Author);
                        bool Break = false;
                        string Reason = "";
                        if (State.LastHelloUserId == UserId)
                        {
                            Break = true;
                            Reason = "has said hello twice in a row";
                        }
                        else if (Context.Message.Attachments.Count > 0)
                        {
                            Break = true;
                            Reason = "has sent an attachment";
                        }
                        else if (Context.Message.Embeds.Count > 0)
                        {
                            Break = true;
                            Reason = "has sent an embed";
                        }
                        else // must be last
                        {
                            string FormattedMessage = new string(Context.Message.Content.ToLower().Where(c => char.IsLetterOrDigit(c)).ToArray());
                            var FoundGreeting = await dbContext.Greetings.ToAsyncEnumerable().Where(g => g.Greeting == FormattedMessage && g.Iteration == State.HelloIteration).FirstOrDefault();
                            if (FoundGreeting != null)
                            {
                                var CopiedUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == FoundGreeting.UserId).FirstOrDefault(); // should exist
                                Break = true;
                                Reason = "has copied <@!" + CopiedUser.DiscordUserId.ToInt64() + "> by saying \"" + Context.Message.Content + "\"";
                            }
                            else
                            {
                                Database.Greetings NewRow = new Database.Greetings();
                                NewRow.UserId = UserId;
                                NewRow.Greeting = FormattedMessage.Substring(0, Math.Min(FormattedMessage.Length, 200));
                                NewRow.Iteration = State.HelloIteration;
                                dbContext.Greetings.Add(NewRow);
                                await dbContext.SaveChangesAsync();
                            }
                        }

                        if (Break)
                        {
                            var DbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == UserId).FirstOrDefault();
                            var BreakerRole = Context.Guild.GetRole(Globals.ChainBreakerRoleId);
                            var BreakerUser = await Context.Client.Rest.GetGuildUserAsync(Globals.MohammadServerId, DbUser.DiscordUserId.ToInt64());
                            await (BreakerUser as IGuildUser).AddRoleAsync(BreakerRole);

                            DbUser.ChainBreaks = DbUser.ChainBreaks + 1;

                            string Result = "";
                            if (State.AutoCreateNewHello)
                                Result = ". A new chain has been created";
                            else
                                Result = ". The channel has been deleted";

                            try
                            {
                                DeleteHelloChannel("<@!" + DbUser.DiscordUserId.ToInt64() + "> " + Reason + Result, dbChannel.DiscordChannelId.ToInt64());
                                dbChannel.Deleted = true;
                                Globals.LogActivity(4, BreakerUser.Username + " " + Reason, "Iteration: " + State.HelloIteration + ", Message: " + Context.Message.Content, true);
                            }
                            catch (Exception e) { Globals.LogActivity(4, BreakerUser.Username + " " + Reason, "Iteration: " + State.HelloIteration + " Error: " + e.Message, false); }

                            try
                            {
                                Globals.AwardChainKeeper(State.HelloIteration, UserId, Context.Guild, Context.Client);
                                Globals.SetSuspendedUser(UserId, Context.Guild, Context.Client);
                            }
                            catch (Exception e)
                            {
                                Globals.LogActivity(1, "Automatic: Failed updating roles after break", "Error: " + e.Message, false);
                                await (Context.Client.GetChannel(Globals.GeneralChannelId) as SocketTextChannel).SendMessageAsync("Failed to update roles.");
                            }

                            State.HelloTimerNotified = false;
                            if (State.AutoCreateNewHello)
                            {
                                try
                                {
                                    State.HelloIteration = State.HelloIteration + 1;
                                    var NewChannel = await Context.Guild.CreateTextChannelAsync("hello-chain-" + State.HelloIteration, x =>
                                    {
                                        x.CategoryId = Globals.HelloCategoryId;
                                        x.Topic = Globals.HelloChannelTopic;
                                    });
                                    State.HelloChannelId = await Globals.GetDbChannelId(NewChannel.Id, NewChannel.Name, 2);
                                    State.HelloDeleted = false;
                                    State.LastHelloUserId = 0;
                                    State.LastHelloMessage = DateTime.Now;
                                    Globals.LogActivity(5, "Automatic: " + BreakerUser.Username + " " + Reason, NewChannel.Name, true);
                                }
                                catch (Exception e) { Globals.LogActivity(5, "Automatic: " + BreakerUser.Username + " " + Reason, "Iteration: " + State.HelloIteration + " Error: " + e.Message, false); }
                            }
                            else
                                State.HelloDeleted = true;

                            await dbContext.SaveChangesAsync();
                        }
                        else
                        {
                            State.LastHelloUserId = UserId;
                            State.LastHelloMessage = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception e) { Globals.LogActivity(1, "UpdateHelloChain", e.Message, false); }
        }

        public async Task HandleCommandAsync(SocketMessage s)
        {
            if (IsReady)
            {
                var msg = s as SocketUserMessage;
                if (msg == null) return;

                if (msg.Author.IsBot == false)
                {
                    var context = new SocketCommandContext(_client, msg);
                    if (!(msg.Channel is SocketDMChannel))
                    {
                        var dbContext = new Database.DatabaseContext();
                        var AppState = await dbContext.AppState.FirstOrDefaultAsync();
                        if (context.Guild.Id == Globals.MohammadServerId) // only track statistics on one server (todo: guilds inside channels)
                            await UpdateSentMessages(context);
                        if (!AppState.HelloDeleted.Value)
                            await UpdateHelloChain(context, AppState);
                        await dbContext.SaveChangesAsync();

                        int argPos = 0;
#if (DEBUG)
                    char Prefix = '?';
#else
                        char Prefix = '$';
#endif

                        if (msg.HasCharPrefix(Prefix, ref argPos))
                        {
                            int UserId = await Globals.GetDbUserId(msg.Author);
                            var result = await _commands.ExecuteAsync(context, argPos, null);

                            if (!result.IsSuccess)
                            {
                                if (result.Error != CommandError.UnknownCommand)
                                {
                                    await context.Channel.SendMessageAsync(result.ErrorReason);
                                    Globals.LogActivity(6, msg.Content, result.ErrorReason, false, UserId);
                                }
                                else
                                {
                                    try
                                    {
                                        bool FoundCommand = false;
                                        string Message = msg.ToString();
                                        if (Message.Length >= 6 && Message.Substring(0, 6) == Prefix + "check")
                                        {
                                            FoundCommand = true;
                                            if (Message.Length > 7)
                                            {
                                                Message = Message.Replace(" ", "");
                                                string CheckingGreeting = new string(Message.Substring(6, Message.Length - 6).ToLower().Where(c => char.IsLetterOrDigit(c)).ToArray());
                                                var CurrentGreetings = await dbContext.Greetings.ToAsyncEnumerable().Where(g => g.Iteration == AppState.HelloIteration).ToList();
                                                if (CurrentGreetings.Exists(g => g.Greeting == CheckingGreeting))
                                                    await context.Channel.SendMessageAsync("That greeting has been used");
                                                else
                                                    await context.Channel.SendMessageAsync("That greeting has not been used");
                                            }
                                            else
                                                throw new Exception("You must specify a greeting to check");
                                        }
                                        else if (Message.Remove(0, 1).All(char.IsNumber))
                                        {
                                            FoundCommand = true;
                                            Int64 ParsedNumber = 0;
                                            bool Parsed = Int64.TryParse(Message.Remove(0, 1), out ParsedNumber);
                                            if (Parsed)
                                            {
                                                if (ParsedNumber > 1000000)
                                                    await context.Channel.SendMessageAsync("THATS A LOTTA MONEY MATE");
                                                else if (ParsedNumber > 1000)
                                                    await context.Channel.SendMessageAsync("OK no need to flex on us");
                                                else
                                                    await context.Channel.SendMessageAsync("ok buddy");
                                            }
                                            else
                                                await context.Channel.SendMessageAsync("ok relax thats too much");
                                        }

                                        if (FoundCommand)
                                            Globals.LogActivity(6, msg.Content, "", FoundCommand, UserId);
                                        //else
                                        //    await context.Channel.SendMessageAsync("Unknown command");
                                    }
                                    catch (Exception e)
                                    {
                                        Globals.LogActivity(6, msg.Content, e.Message, false, UserId);
                                        await context.Channel.SendMessageAsync("Operation failed: " + e.Message);
                                    }
                                }
                            }
                            else
                                Globals.LogActivity(6, msg.Content, "", true, UserId);
                        }
                    }
                    else
                    {
                        int UserId = await Globals.GetDbUserId(msg.Author);
                        try
                        {
                            if (msg.Author.Id != Globals.BeefBossId && !msg.Author.IsBot)
                            {
                                await context.Channel.SendMessageAsync("Noted.");
                                var Beef = await _client.Rest.GetUserAsync(Globals.BeefBossId);
                                await Beef.SendMessageAsync("<@!" + msg.Author.Id + ">" + " at " + msg.Timestamp + " said: " + msg);
                            }
                            else
                            {
                                string Message = msg.ToString();
                                if (Message.Contains("$reply "))
                                {
                                    string SendingMessage = Message.Substring(7, Message.Length - 7);

                                    var DMChannel = msg.Channel as SocketDMChannel;
                                    var LastMessages = await DMChannel.GetMessagesAsync(2).Flatten().ToList();
                                    if (LastMessages[1].Content.Split("<")[1][0] == '@')
                                    {
                                        var SendingUser = await _client.Rest.GetGuildUserAsync(Globals.MohammadServerId, ulong.Parse(LastMessages[1].Content.Split("!")[1].Split(">")[0]));
                                        await SendingUser.SendMessageAsync(SendingMessage);
                                        var Beef = await _client.Rest.GetUserAsync(Globals.BeefBossId);
                                        await Beef.SendMessageAsync("Replied");
                                    }
                                }
                            }
                        }
                        catch (Exception e) { Globals.LogActivity(1, "Processing DM", "Message: " + msg.Content + " Error: " + e.Message, false, UserId); }
                    }
                }
            }

        }


    }


}


