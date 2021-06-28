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
    public class Features : ModuleBase<SocketCommandContext>
    {
        [Command("setfield")]
        public async Task Task1(string Field, string Params)
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");

            var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

            try
            {
                switch (Field)
                {
                    case "ResponseChannelId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var ChannelId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.ResponseChannelId = ChannelId;
                    } break;
                    case "HelloCategoryId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var CategoryId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.HelloCategoryId = CategoryId;
                    } break;
                    case "ChainBreakerRoleId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var RoleId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.ChainBreakerRoleId = RoleId;
                    } break;
                    case "ChainKeeperRoleId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var RoleId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.ChainKeeperRoleId = RoleId;
                    } break;
                    case "SuppressedRoleId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var RoleId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.SuppressedRoleId = RoleId;
                    } break;
                    case "SuspendedRoleId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var RoleId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.SuspendedRoleId = RoleId;
                    } break;
                    case "JoinAutoRoleId":
                    {
                        string FormattedId = new string(Params.Where(char.IsNumber).ToArray());
                        var RoleId = ulong.Parse(FormattedId).ToByteArray();
                        AppState.JoinAutoRoleId = RoleId;
                    } break;
                    default:
                        { throw new Exception("Invalid field name"); }
                }
            }
            catch (Exception e) { throw new Exception("Failed; " + e.Message); }

            await dbContext.SaveChangesAsync();
            await ReplyAsync("Field successfully changed");
        }

        [Command("feature")]
        public async Task Task2(string Feature, bool Enabled)
        {
            var GuildUser = Context.Guild.GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
                throw new Exception("You do not have permission to run that command");

            var dbContext = await DbHelper.GetDbContext(Context.Guild.Id);
            var AppState = await dbContext.AppStates.AsAsyncEnumerable().FirstOrDefaultAsync();

            switch (Feature)
            {
                case "HelloChain":
                {
                    AppState.EnableHelloChain = Enabled;
                } break;
                case "StatisticsTracking":
                {
                    AppState.EnableStatisticsTracking = Enabled;
                } break;
                case "MeCommand":
                {
                    AppState.EnableMeCommand = Enabled;
                } break;
                case "Emotes":
                {
                    AppState.EnableEmotes = Enabled;
                } break;
                default:
                    { throw new Exception("Invalid feature name"); }
            }

            await dbContext.SaveChangesAsync();
            await ReplyAsync("Feature " + (Enabled ? "enabled" : "disabled"));
        }
    }
}
