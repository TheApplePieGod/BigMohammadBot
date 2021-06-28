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
using Discord.Audio;
using System.Diagnostics;

namespace BigMohammadBot
{
    public class CommandHandler
    {
        public static DiscordSocketClient _client;

        private CommandService _commands;

        private const int GenericUpdateDelay = 10; // seconds
        private System.Threading.Timer HelloChainTimer;
        private System.Threading.Timer GenericUpdateTimer;
        private System.Threading.Timer SparseUpdateTimer;
        private bool IsReady = false;
        private ulong LastRPSUser = 0;
        private int LastRPSInput = 0; // 0: rock, 1: paper, 2: scissors
        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _commands = new CommandService();

            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            _client.MessageReceived += HandleCommandAsync;
            _client.MessageDeleted += OnMessageDeleted;

            _client.ReactionAdded += OnReactionAdded;
            _client.ReactionRemoved += OnReactionRemoved;

            _client.UserJoined += OnUserJoined;

            _client.Ready += Ready;
        }

        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction)
        {

        }

        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction)
        {

        }

        public async Task OnMessageDeleted(Cacheable<IMessage, ulong> Message, ISocketMessageChannel Channel)
        {
            var GuildChannel = Channel as SocketGuildChannel;
            if (GuildChannel.Guild != null)
            {
                var dbContext = await DbHelper.GetDbContext(GuildChannel.Guild.Id);

                //var ReactionRoles = await dbContext.
                //foreach (var elem in dbContext.)
            }
        }

        public async Task OnUserJoined(SocketGuildUser User)
        {
            var dbContext = await DbHelper.GetDbContext(User.Guild.Id);
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

            if (AppState.JoinMuteMinutes > 0 && AppState.SuppressedRoleId != null && AppState.SuppressedRoleId.Length > 0)
            {
                int UserId = await Globals.GetDbUserId(User.Guild.Id, User);
                var SuppressedUserRow = await dbContext.SupressedUsers.AsAsyncEnumerable().Where(u => u.UserId == UserId).FirstOrDefaultAsync();

                if (SuppressedUserRow == null)
                {
                    Database.SupressedUser NewRow = new Database.SupressedUser();
                    NewRow.UserId = UserId;
                    NewRow.TimeStarted = DateTime.Now;
                    NewRow.MaxTimeSeconds = AppState.JoinMuteMinutes * 60;
                    dbContext.SupressedUsers.Add(NewRow);
                }
                else
                {
                    SuppressedUserRow.TimeStarted = DateTime.Now;
                    SuppressedUserRow.MaxTimeSeconds = AppState.JoinMuteMinutes * 60;
                }

                var SuppressRole = User.Guild.GetRole(AppState.SuppressedRoleId.ToInt64());
                await User.AddRoleAsync(SuppressRole);

                await dbContext.SaveChangesAsync();
            }

            if (AppState.JoinAutoRoleId != null && AppState.JoinAutoRoleId.Length > 0)
            {
                var AutoRole = User.Guild.GetRole(AppState.JoinAutoRoleId.ToInt64());
                await User.AddRoleAsync(AutoRole);
            }
        }

        public async Task Ready()
        {
            await Initialize();
            IsReady = true;
        }

        public async void DeleteHelloChannel(SocketTextChannel ResponseChannel, string Message, ulong HelloChannelId)
        {
            var HelloChannel = _client.GetChannel(HelloChannelId) as SocketTextChannel;
            if (HelloChannel != null && ResponseChannel != null)
            {
                await HelloChannel.DeleteAsync();
                await ResponseChannel.SendMessageAsync(Message);
            }
        }

        public async void SparseTimedUpdate(object state) // runs every 12 hours
        {
            foreach (var entry in DbHelper.ContextLoaded)
            {
                var dbContext = await DbHelper.GetDbContext(entry.Key);
                var AllChannels = _client.GetGuild(entry.Key).Channels;
                var AllDbChannels = await dbContext.Channels.AsAsyncEnumerable().ToListAsync();
                foreach (Database.Channel Channel in AllDbChannels)
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
        }

        public async void HelloChainTimerUpdate(object state)
        {
            foreach (var entry in DbHelper.ContextLoaded)
            {
                try
                {
                    var dbContext = await DbHelper.GetDbContext(entry.Key);
                    var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

                    if (AppState.EnableHelloChain && !AppState.HelloDeleted.Value && AppState.HelloChannelId != 0)
                    {
                        DateTime CurrentTime = DateTime.Now;
                        TimeSpan TimeDifference = (CurrentTime - AppState.LastHelloMessage.Value);

                        var ResponseChannel = _client.GetGuild(entry.Key).DefaultChannel;
                        if (AppState.ResponseChannelId != null && AppState.ResponseChannelId.Length > 0)
                            ResponseChannel = _client.GetChannel(AppState.ResponseChannelId.ToInt64()) as SocketTextChannel;

                        if (TimeDifference.TotalHours >= 12)
                        {
                            try
                            {
                                var dbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(c => c.Id == AppState.HelloChannelId).FirstOrDefaultAsync();

                                DeleteHelloChannel(ResponseChannel, "A hello has not been chained for more than a day. The channel has been deleted.", dbChannel.DiscordChannelId.ToInt64());

                                dbChannel.Deleted = true;
                                AppState.HelloDeleted = true;
                                AppState.HelloTimerNotified = false;

                                await dbContext.SaveChangesAsync();

                                Globals.AwardChainKeeper(ResponseChannel, AppState.HelloIteration, 0, _client.GetGuild(entry.Key), _client);

                                Globals.LogActivity(entry.Key, 4, "Surpassed 12 hours", "", true);
                            }
                            catch (Exception e) { Globals.LogActivity(entry.Key, 4, "Surpassed 12 hours", e.Message, false); }
                        }
                        else if (!AppState.HelloTimerNotified.Value && TimeDifference.TotalHours >= 11)
                        {
                            await ResponseChannel.SendMessageAsync("Hello chain will be deleted in an hour or less, make sure to refresh the timer");

                            AppState.HelloTimerNotified = true;
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception e) { Globals.LogActivity(entry.Key, 1, "HelloChainTimerUpdate", e.Message, false); }
            }
        }

        public async void GenericTimedUpdate(object state)
        {
            foreach (var entry in DbHelper.ContextLoaded)
            {
                try
                {
                    var dbContext = await DbHelper.GetDbContext(entry.Key);
                    var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

                    if (AppState.EnableStatisticsTracking)
                    {
                        var VoiceChannels = _client.GetGuild(entry.Key).VoiceChannels;
                        foreach (SocketVoiceChannel Channel in VoiceChannels)
                        {
                            if (Channel.Users.Count > 0)
                            {
                                foreach (SocketGuildUser User in Channel.Users)
                                {
                                    if (!User.IsBot && !User.IsSelfMuted && !User.IsSelfDeafened && !User.IsMuted && !User.IsDeafened)
                                    {
                                        DateTime CurrentWeekDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                                        int UserId = await Globals.GetDbUserId(entry.Key, User);
                                        int ChannelId = await Globals.GetDbChannelId(Channel);
                                        var VoiceStatisticsRow = await dbContext.VoiceStatistics.ToAsyncEnumerable().Where(u => u.UserId == UserId && u.ChannelId == ChannelId && u.TimePeriod == CurrentWeekDate).FirstOrDefaultAsync();

                                        if (VoiceStatisticsRow == null)
                                        {
                                            Database.VoiceStatistic NewRow = new Database.VoiceStatistic();
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
                    }

                    var SuppressedUsers = await dbContext.SupressedUsers.AsAsyncEnumerable().ToListAsync();
                    if (SuppressedUsers.Count > 0 && AppState.SuppressedRoleId != null && AppState.SuppressedRoleId.Length > 0)
                    {
                        var SuppressRole = _client.GetGuild(entry.Key).GetRole(AppState.SuppressedRoleId.ToInt64());
                        foreach (Database.SupressedUser SuppressedUser in SuppressedUsers)
                        {
                            double SecondsDifference = (DateTime.Now - SuppressedUser.TimeStarted.Value).TotalSeconds;
                            if (SecondsDifference >= SuppressedUser.MaxTimeSeconds)
                            {
                                var User = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == SuppressedUser.UserId).FirstOrDefaultAsync(); // should exist at this point
                                try
                                {
                                    ulong DiscordId = User.DiscordUserId.ToInt64();
                                    var GuildUser = await _client.Rest.GetGuildUserAsync(entry.Key, DiscordId);
                                    var Guild = _client.GetGuild(GuildUser.GuildId);
                                    await GuildUser.RemoveRoleAsync(SuppressRole);
                                    if (AppState.ResponseChannelId != null && AppState.ResponseChannelId.Length > 0)
                                        await (_client.GetChannel(AppState.ResponseChannelId.ToInt64()) as ITextChannel).SendMessageAsync("<@!" + DiscordId + "> has been unmuted");
                                    else
                                        await Guild.DefaultChannel.SendMessageAsync("<@!" + DiscordId + "> has been unmuted");
                                    dbContext.SupressedUsers.Remove(SuppressedUser);
                                    Globals.LogActivity(entry.Key, 2, User.DiscordUserName, "", true);
                                }
                                catch (Exception e) { Globals.LogActivity(entry.Key, 2, User.DiscordUserName, e.Message, false); }
                            }
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
                catch (Exception e) { Globals.LogActivity(entry.Key, 1, "GenericTimedUpdate", e.Message, false); }
            }
        }

        public async Task Initialize()
        {
            //👀
            Game Activity = new Game("👀 ($help)", ActivityType.Watching);
            await _client.SetActivityAsync(Activity);

            HelloChainTimer = new System.Threading.Timer(HelloChainTimerUpdate, null, 0, 1000 * 3);
            GenericUpdateTimer = new System.Threading.Timer(GenericTimedUpdate, null, 0, 1000 * GenericUpdateDelay);
            SparseUpdateTimer = new System.Threading.Timer(SparseTimedUpdate, null, 0, 1000 * 60 * 60 * 12); // 12 hours
        }

        public async Task UpdateSentMessages(SocketCommandContext Context)
        {
            try
            {
                DateTime CurrentWeekDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
                int UserId = await Globals.GetDbUserId(Context.Guild.Id, Context.Message.Author);
                int ChannelId = await Globals.GetDbChannelId(Context.Guild.GetChannel(Context.Channel.Id)); // context.channel should always be in a guild here
                var MessageStatisticsRow = await dbContext.MessageStatistics.ToAsyncEnumerable().Where(u => u.UserId == UserId && u.ChannelId == ChannelId && u.TimePeriod == CurrentWeekDate).FirstOrDefaultAsync();

                if (MessageStatisticsRow == null)
                {
                    Database.MessageStatistic NewRow = new Database.MessageStatistic();
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
            catch (Exception e) { Globals.LogActivity(Context.Guild.Id, 1, "UpdateSentMessages", e.Message, false); }
        }

        public async Task UpdateHelloChain(SocketCommandContext Context, Database.AppState State)
        {
            var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
            try
            {
                if (State.HelloChannelId != 0)
                {
                    var dbChannel = await dbContext.Channels.ToAsyncEnumerable().Where(c => c.Id == State.HelloChannelId).FirstOrDefaultAsync(); // todo: change back to id instead of channel reference for performance?
                    if (Context.Message.Channel.Id == dbChannel.DiscordChannelId.ToInt64())
                    {
                        int UserId = await Globals.GetDbUserId(Context.Guild.Id, Context.Message.Author);
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
                            string MessageWithSpaces = new string(Context.Message.Content.ToLower().Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim();
                            var FoundGreeting = await dbContext.Greetings.ToAsyncEnumerable().Where(g => g.Iteration == State.HelloIteration && g.Greeting1.Replace(" ", "") == FormattedMessage).FirstOrDefaultAsync();
                            if (FoundGreeting != null)
                            {
                                var CopiedUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == FoundGreeting.UserId).FirstOrDefaultAsync(); // should exist
                                Break = true;
                                Reason = "has copied <@!" + CopiedUser.DiscordUserId.ToInt64() + "> by saying \"" + Context.Message.Content + "\"";
                            }
                            else
                            {
                                Database.Greeting NewRow = new Database.Greeting();
                                NewRow.UserId = UserId;
                                NewRow.Greeting1 = MessageWithSpaces.Substring(0, Math.Min(MessageWithSpaces.Length, 200));
                                NewRow.Iteration = State.HelloIteration;
                                dbContext.Greetings.Add(NewRow);
                                await dbContext.SaveChangesAsync();
                            }
                        }

                        State.HelloTimerNotified = false;

                        if (Break)
                        {
                            var DbUser = await dbContext.Users.ToAsyncEnumerable().Where(u => u.Id == UserId).FirstOrDefaultAsync();
                            var BreakerUser = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, DbUser.DiscordUserId.ToInt64());
                            var ResponseChannel = Context.Guild.DefaultChannel;
                            if (State.ResponseChannelId != null && State.ResponseChannelId.Length > 0)
                                ResponseChannel = _client.GetChannel(State.ResponseChannelId.ToInt64()) as SocketTextChannel;

                            if (State.ChainBreakerRoleId != null && State.ChainBreakerRoleId.Length > 0)
                            {
                                try
                                {
                                    var BreakerRole = Context.Guild.GetRole(State.ChainBreakerRoleId.ToInt64());
                                    await (BreakerUser as IGuildUser).AddRoleAsync(BreakerRole);
                                }
                                catch { }
                            }

                            DbUser.ChainBreaks = DbUser.ChainBreaks + 1;

                            string Result = "";
                            if (State.AutoCreateNewHello)
                                Result = ". A new chain has been created";
                            else
                                Result = ". The channel has been deleted";

                            try
                            {
                                DeleteHelloChannel(ResponseChannel, "<@!" + DbUser.DiscordUserId.ToInt64() + "> " + Reason + Result, dbChannel.DiscordChannelId.ToInt64());
                                dbChannel.Deleted = true;
                                Globals.LogActivity(Context.Guild.Id, 4, BreakerUser.Username + " " + Reason, "Iteration: " + State.HelloIteration + ", Message: " + Context.Message.Content, true);
                            }
                            catch (Exception e) { Globals.LogActivity(Context.Guild.Id, 4, BreakerUser.Username + " " + Reason, "Iteration: " + State.HelloIteration + " Error: " + e.Message, false); }

                            try
                            {
                                Globals.AwardChainKeeper(ResponseChannel, State.HelloIteration, UserId, Context.Guild, Context.Client);
                                Globals.SetSuspendedUser(ResponseChannel, UserId, Context.Guild, Context.Client);
                            }
                            catch (Exception e)
                            {
                                Globals.LogActivity(Context.Guild.Id, 1, "Automatic: Failed updating roles after break", "Error: " + e.Message, false);
                                await ResponseChannel.SendMessageAsync("Failed to update roles.");
                            }

                            if (State.AutoCreateNewHello)
                            {
                                try
                                {
                                    State.HelloIteration = State.HelloIteration + 1;
                                    var NewChannel = await Context.Guild.CreateTextChannelAsync("hello-chain-" + State.HelloIteration, x =>
                                    {
                                        if (State.HelloCategoryId != null && State.HelloCategoryId.Length > 0)
                                            x.CategoryId = State.HelloCategoryId.ToInt64();
                                        x.Topic = State.HelloTopic;
                                    });
                                    State.HelloChannelId = await Globals.GetDbChannelId(Context.Guild.Id, NewChannel.Id, NewChannel.Name, 2);
                                    State.HelloDeleted = false;
                                    State.LastHelloUserId = 0;
                                    State.LastHelloMessage = DateTime.Now;
                                    Globals.LogActivity(Context.Guild.Id, 5, "Automatic: " + BreakerUser.Username + " " + Reason, NewChannel.Name, true);
                                }
                                catch (Exception e) { Globals.LogActivity(Context.Guild.Id, 5, "Automatic: " + BreakerUser.Username + " " + Reason, "Iteration: " + State.HelloIteration + " Error: " + e.Message, false); }
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
            catch (Exception e) { Globals.LogActivity(Context.Guild.Id, 1, "UpdateHelloChain", e.Message, false); }
        }

        ulong PickWinner(ulong id1, ulong id2, int input1, int input2)
        {
            if (input1 == input2)
                return 1;
            // 0 rock 1 paper 2 scissors
            if (input1 == 0 && input2 == 1 || input1 == 1 && input2 == 2 || input1 == 2 && input2 == 0)
                return id2;
            else
                return id1;
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
                        var dbContext = await DbHelper.GetDbContext(context.Guild.Id);
                        var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();
                        if (AppState.EnableStatisticsTracking)
                            await UpdateSentMessages(context);
                        if (AppState.EnableHelloChain && !AppState.HelloDeleted.Value)
                            await UpdateHelloChain(context, AppState);
                        await dbContext.SaveChangesAsync();

                        int argPos = 0;

                        string Message = msg.ToString();

                        if (AppState.EnableEmotes)
                        {
                            if (Message.Count(c => c == '$') > 1) // possibly contains emotes
                            {
                                var AllEmotes = await dbContext.Emotes.AsAsyncEnumerable().ToListAsync();
                                var Splits = Message.Split('$');
                                for (int i = 1; i < Splits.Length - 1; i++) // skip the first and last splits
                                {
                                    string Split = Splits[i].Trim().ToLower();
                                    var FoundEmote = AllEmotes.Find(e => e.Name == Split);
                                    if (FoundEmote != null)
                                    {
                                        await context.Channel.SendMessageAsync(FoundEmote.Link);
                                        break;
                                    }
                                }
                            }
                        }

                        if (msg.HasCharPrefix(Globals.CommandPrefix, ref argPos))
                        {
                            int UserId = await Globals.GetDbUserId(context.Guild.Id, msg.Author);
                            var result = await _commands.ExecuteAsync(context, argPos, null);

                            if (!result.IsSuccess)
                            {
                                if (result.Error != CommandError.UnknownCommand)
                                {
                                    await context.Channel.SendMessageAsync(result.ErrorReason);
                                    Globals.LogActivity(context.Guild.Id, 6, msg.Content, result.ErrorReason, false, UserId);
                                }
                                else
                                {
                                    try
                                    {
                                        bool FoundCommand = false;
                                        if (AppState.EnableHelloChain && Message.Length >= 6 && Message.Substring(0, 6) == Globals.CommandPrefix + "check")
                                        {
                                            FoundCommand = true;
                                            if (Message.Length > 7)
                                            {
                                                Message = Message.Replace(" ", "");
                                                string CheckingGreeting = new string(Message.Substring(6, Message.Length - 6).ToLower().Where(c => char.IsLetterOrDigit(c)).ToArray());
                                                var FoundGreeting = await dbContext.Greetings.ToAsyncEnumerable().Where(g => g.Iteration == AppState.HelloIteration && g.Greeting1.Replace(" ", "") == CheckingGreeting).FirstOrDefaultAsync();
                                                if (FoundGreeting != null)
                                                    await context.Channel.SendMessageAsync("That greeting has been used");
                                                else
                                                    await context.Channel.SendMessageAsync("That greeting has not been used");
                                            }
                                            else
                                                throw new Exception("You must specify a greeting to check");
                                        }
                                        else if (Message.Remove(0, 1).All(char.IsNumber))
                                        {
                                            //FoundCommand = true;
                                            //Int64 ParsedNumber = 0;
                                            //bool Parsed = Int64.TryParse(Message.Remove(0, 1), out ParsedNumber);
                                            //if (Parsed)
                                            //{
                                            //    if (ParsedNumber > 1000000)
                                            //        await context.Channel.SendMessageAsync("THATS A LOTTA MONEY MATE");
                                            //    else if (ParsedNumber > 1000)
                                            //        await context.Channel.SendMessageAsync("OK no need to flex on us");
                                            //    else
                                            //        await context.Channel.SendMessageAsync("ok buddy");
                                            //}
                                            //else
                                            //    await context.Channel.SendMessageAsync("ok relax thats too much");
                                        }

                                        if (FoundCommand)
                                            Globals.LogActivity(context.Guild.Id, 6, msg.Content, "", FoundCommand, UserId);
                                        //else
                                        //    await context.Channel.SendMessageAsync("Unknown command");
                                    }
                                    catch (Exception e)
                                    {
                                        Globals.LogActivity(context.Guild.Id, 6, msg.Content, e.Message, false, UserId);
                                        await context.Channel.SendMessageAsync("Operation failed: " + e.Message);
                                    }
                                }
                            }
                            else
                                Globals.LogActivity(context.Guild.Id, 6, msg.Content, "", true, UserId);
                        }
                    }
                    else
                    {
                        try
                        {
                            if (msg.Content.Trim() == "rock" || msg.Content.Trim() == "scissors" || msg.Content.Trim() == "paper")
                            {
                                ulong winner = 0;
                                switch (msg.Content.Trim())
                                {
                                    case "rock":
                                        if (LastRPSUser == 0)
                                        {
                                            LastRPSUser = msg.Author.Id;
                                            LastRPSInput = 0;
                                        }
                                        else
                                        {
                                            winner = PickWinner(msg.Author.Id, LastRPSUser, 0, LastRPSInput);
                                        }
                                        break;
                                    case "paper":
                                        if (LastRPSUser == 0)
                                        {
                                            LastRPSUser = msg.Author.Id;
                                            LastRPSInput = 1;
                                        }
                                        else
                                        {
                                            winner = PickWinner(msg.Author.Id, LastRPSUser, 1, LastRPSInput);
                                        }
                                        break;
                                    case "scissors":
                                        if (LastRPSUser == 0)
                                        {
                                            LastRPSUser = msg.Author.Id;
                                            LastRPSInput = 2;
                                        }
                                        else
                                        {
                                            winner = PickWinner(msg.Author.Id, LastRPSUser, 2, LastRPSInput);
  
                                        }
                                        break;
                                }

                                if (winner != 0)
                                {
                                    var User1 = await _client.Rest.GetUserAsync(msg.Author.Id);
                                    var User2 = await _client.Rest.GetUserAsync(LastRPSUser);

                                    if (winner != 1)
                                    {
                                        await User1.SendMessageAsync("Between you and <@!" + LastRPSUser + ">, <@!" + winner + "> has won.");
                                        await User2.SendMessageAsync("Between you and <@!" + msg.Author.Id + ">, <@!" + winner + "> has won.");
                                    }
                                    else // tie
                                    {
                                        await User1.SendMessageAsync("Between you and <@!" + LastRPSUser + ">, you tied.");
                                        await User2.SendMessageAsync("Between you and <@!" + msg.Author.Id + ">, you tied.");
                                    }

                                    LastRPSInput = 0;
                                    LastRPSUser = 0;
                                }
                            }
                            else
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
                                        var LastMessages = await DMChannel.GetMessagesAsync(2).Flatten().ToListAsync();
                                        if (LastMessages[1].Content.Split("<")[1][0] == '@')
                                        {
                                            var SendingUser = await _client.Rest.GetUserAsync(ulong.Parse(LastMessages[1].Content.Split("!")[1].Split(">")[0]));
                                            await SendingUser.SendMessageAsync(SendingMessage);
                                            var Beef = await _client.Rest.GetUserAsync(Globals.BeefBossId);
                                            await Beef.SendMessageAsync("Replied");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e) { /*Globals.LogActivity(1, "Processing DM", "Message: " + msg.Content + " Error: " + e.Message, false, UserId);*/ }
                        // dm logging disabled for now because there is no 'central' db to store this log in
                    }
                }
            }

        }
    }
}


