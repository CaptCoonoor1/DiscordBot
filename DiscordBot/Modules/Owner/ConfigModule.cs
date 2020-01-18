﻿using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DiscordBot.Common.Preconditions;
using DiscordBot.Common;
using DiscordBot.Extensions;
using DiscordBot.Logging;
using DiscordBot.Objects;
using MelissaNet;

namespace DiscordBot.Modules.Owner
{
    [Name("Configuration Commands")]
    [MinPermissions(PermissionLevel.BotOwner)]
    public class ConfigModule : ModuleBase
    {
        [Group("editconfig")]
        public class ConfigurationModule : ModuleBase
        {
            [Command("")]
            public async Task SendSyntax()
            {
                await ReplyAsync("**Syntax:** " +
                                 Guild.Load(Context.Guild.Id).Prefix + "editconfig [command] [command syntax]\n```INI\n" +
                                 "Available Commands\n" +
                                 "-----------------------------\n" +
                                 "[ 1] clearactivity\n" +
                                 "[ 2] setgame [type] [message]\n" +
                                 "[a.] [type] -> [none] [playing] [listening] [watching]\n" +
                                 "[ 3] setstreaming [stream url] [message]\n" +
                                 "[ 4] status [status]\n" +
                                 "[a.] [status] -> [online] [donotdisturb] [idle] [invisible]\n" +
                                 "[ 5] toggleunknowncommand\n" +
                                 "[ 6] leaderboardamount [number of users to display]\n" +
                                 "[ 7] quotelevel [level]\n" +
                                 "[ 8] prefixlevel [level]\n" +
                                 "[ 9] rgblevel [level]\n" +
                                 "[10] senpaichance [number 1-100]\n" +
                                 "[11] globallogchannel [channel mention / channel id]\n" +
                                 "[12] rule34 [max number for random to use]\n" +
                                 "[13] minlengthforexp [string length for exp gain]\n" +
                                 "[14] leaderboardtrophyurl [link]\n" +
                                 "[15] leaderboardembedcolor [uint id]\n" +
                                 "[16] toggleexpawarding\n" +
                                 "[17] toggleshowallawards\n" +
                                 "[18] awardsiconurl [link]\n" +
                                 "[19] toggleawardingexpmention\n" +
                                 "[20] toggleexpreactawarding\n" +
                                 "[21] toggleexpreactpostawarding\n" +
                                 "```");
                
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
            }
            
            [Command("clearactivity"), Summary("Changes the game message of the bot.")]
            public async Task ClearActivity()
            {
                IActivity activity = new Game("");
                await DiscordBot.Bot.SetActivityAsync(activity);
                Configuration.UpdateConfiguration(activityName: activity.Name, activityType: (int)activity.Type);
                
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                var eb = new EmbedBuilder()
                    .WithDescription(Context.User.Username + " cleared " + DiscordBot.Bot.CurrentUser.Mention + "'s activity message.")
                    .WithColor(Color.DarkGreen);

                await ReplyAsync("", false, eb.Build());
            }

            [Command("setgame"), Summary("Changes the game message of the bot.")]
            public async Task SetActivityGame(string activityType = null, [Remainder]string activityMessage = null)
            {
                if (activityType == null || activityMessage == null)
                {
                    await ReplyAsync("**Syntax:** " + Guild.Load(Context.Guild.Id).Prefix + "editconfig setgame [type] [message]");
                    return;
                }

                IActivity activity = new Game("");
                switch (activityType.ToUpperInvariant())
                {
                    case "PLAYING":
                        activity = new Game(activityMessage);
                        break;
                    case "LISTENING":
                        activity = new Game(activityMessage, ActivityType.Listening);
                        break;
                    case "WATCHING": 
                        activity = new Game(activityMessage, ActivityType.Watching);
                        break;
                    case "STREAMING":
                        await ReplyAsync(Context.User.Mention +
                                         ", in order to be streaming, please use 'setstreaming' instead of 'setgame', amending the parameters to fit.");
                        return;
                    default:
                        await ReplyAsync(Context.User.Mention +
                                         ", there was an error trying to select the ActivityType, please check your parameters and try again.");
                        return;
                }
                await DiscordBot.Bot.SetActivityAsync(activity);
                Configuration.UpdateConfiguration(activityName: activity.Name, activityType: (int)activity.Type, activityStream: null);
                
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                var eb = new EmbedBuilder()
                    .WithDescription(Context.User.Username + " updated " + DiscordBot.Bot.CurrentUser.Mention + "'s activity message.")
                    .WithColor(Color.DarkGreen);

                await ReplyAsync("", false, eb.Build());
            }
            
            [Command("setstreaming"), Summary("Changes the game message of the bot.")]
            public async Task SetActivityStream(string streamUrl = null, [Remainder]string activityMessage = null)
            {
                if (streamUrl == null || activityMessage == null)
                {
                    await ReplyAsync("**Syntax:** " + Guild.Load(Context.Guild.Id).Prefix + "editconfig setstreaming [stream url] [message]");
                    return;
                }

                IActivity activity = new StreamingGame(activityMessage, streamUrl);
                await DiscordBot.Bot.SetActivityAsync(activity);
                Configuration.UpdateConfiguration(activityName: activity.Name, activityType: (int)activity.Type, activityStream: streamUrl);
                
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                var eb = new EmbedBuilder()
                    .WithDescription(Context.User.Username + " updated " + DiscordBot.Bot.CurrentUser.Mention + "'s activity message.")
                    .WithColor(Color.DarkGreen);

                await ReplyAsync("", false, eb.Build());
            }
            
            [Group("status")]
            public class StatusModule : ModuleBase
            {
                [Command("online"), Summary("Sets the bot's status to online.")]
                [Alias("active", "green")]
                public async Task SetOnline()
                {
                    Configuration.UpdateConfiguration(status: UserStatus.Online);
                    await DiscordBot.Bot.SetStatusAsync(UserStatus.Online);
                    AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                    
                    await ReplyAsync("Status updated to Online, " + Context.User.Mention);
                }

                [Command("donotdisturb"), Summary("Sets the bot's status to do not disturb.")]
                [Alias("dnd", "disturb", "red")]
                public async Task SetBusy()
                {
                    Configuration.UpdateConfiguration(status: UserStatus.DoNotDisturb);
                    await DiscordBot.Bot.SetStatusAsync(UserStatus.DoNotDisturb);
                    AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                    
                    await ReplyAsync("Status updated to Do Not Disturb, " + Context.User.Mention);
                }

                [Command("idle"), Summary("Sets the bot's status to idle.")]
                [Alias("afk", "yellow")]
                public async Task SetIdle()
                {
                    Configuration.UpdateConfiguration(status: UserStatus.AFK);
                    await DiscordBot.Bot.SetStatusAsync(UserStatus.AFK);
                    AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                    
                    await ReplyAsync("Status updated to Idle, " + Context.User.Mention);
                }

                [Command("invisible"), Summary("Sets the bot's status to invisible.")]
                [Alias("hidden", "offline", "grey")]
                public async Task SetInvisible()
                {
                    Configuration.UpdateConfiguration(status: UserStatus.Invisible);
                    await DiscordBot.Bot.SetStatusAsync(UserStatus.Invisible);
                    AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                    
                    await ReplyAsync("Status updated to Invisible, " + Context.User.Mention);
                }
            }

            [Command("toggleunknowncommand"), Summary("Toggles the unknown command message.")]
            public async Task ToggleUc()
            {
                Configuration.UpdateConfiguration(unknownCommandEnabled: !Configuration.Load().UnknownCommandEnabled);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("UnknownCommand has been toggled by " + Context.User.Mention + " (enabled: " + Configuration.Load().UnknownCommandEnabled.ToYesNo() + ")");
            }

            [Command("leaderboardamount"), Summary("Set the amount of users who show up in the leaderboards.")]
            public async Task SetLeaderboardAmount(int value)
            {
                int oldValue = Configuration.Load().LeaderboardAmount;
                Configuration.UpdateConfiguration(leaderboardAmount: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated the Leaderboard amount to: " + value + " (was: " + oldValue + ")");
            }

            [Command("quotelevel"), Summary("")]
            public async Task ChangeQuotePrice(int levelRequirement)
            {
                int oldLevel = Configuration.Load().QuoteLevelRequirement;
                Configuration.UpdateConfiguration(quoteLevelRequirement: levelRequirement);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("**" + Context.User.Mention + "** has updated the quote level to **" + levelRequirement + "**. (Was: **" + oldLevel + "**)");
            }

            [Command("prefixlevel"), Summary("")]
            public async Task ChangePrefixPrice(int levelRequirement)
            {
                int oldLevel = Configuration.Load().PrefixLevelRequirement;
                Configuration.UpdateConfiguration(prefixLevelRequirement: levelRequirement);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("**" + Context.User.Mention + "** has updated the prefix level to **" + levelRequirement + "** coins. (Was: **" + oldLevel + "**)");
            }

            [Command("rgblevel"), Summary("")]
            public async Task ChangeRGBPrice(int levelRequirement)
            {
                int oldLevel = Configuration.Load().RGBLevelRequirement;
                Configuration.UpdateConfiguration(rgbLevelRequirement: levelRequirement);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("**" + Context.User.Mention + "** has updated the RGB level to **" + levelRequirement + "** coins. (Was: **" + oldLevel + "**)");
            }

            [Command("senpaichance"), Summary("")]
            public async Task ChangeSenpaiChance(int chanceValue)
            {
                int oldChance = Configuration.Load().SenpaiChanceRate;
                Configuration.UpdateConfiguration(senpaiChanceRate: chanceValue);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("**" + Context.User.Mention + "** has updated the senpai chance to **" + chanceValue + "%**. (Was: **" + oldChance + "%**)");
            }

            [Command("globallogchannel"), Summary("")]
            public async Task SetGlobalLogChannel(SocketTextChannel channel)
            {
                Configuration.UpdateConfiguration(logChannelId: channel.Id);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated \"LogChannelID\" to: " + channel.Mention);
            }

            [Command("rule34"), Summary("Set the max random value for the Rule34 Gamble.")]
            public async Task SetRule34Max(int value)
            {
                int oldValue = Configuration.Load().MaxRuleXGamble;
                Configuration.UpdateConfiguration(maxRuleXGamble: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated the Rule34 Max to: " + value + " (was: " + oldValue + ")");
            }

            [Command("minlengthforexp"), Summary("Set the required length of a message for a user to receive (a) coin(s).")]
            public async Task SetRequiredMessageLengthForCoins(int value)
            {
                int oldValue = Configuration.Load().MinLengthForEXP;
                Configuration.UpdateConfiguration(minLengthForEXP: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);

                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated the MinLengthForCoin amount to: " + value + " (was: " + oldValue + ")");
            }

            [Command("toggleexpawarding"), Summary("Toggles if users receive EXP.")]
            public async Task ToggleEXPAwarding()
            {
                Configuration.UpdateConfiguration(awardingEXPEnabled: !Configuration.Load().AwardingEXPEnabled);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("EXP awarding has been toggled by " + Context.User.Mention + " (enabled: " + Configuration.Load().AwardingEXPEnabled.ToYesNo() + ")");
            }
            
            [Command("toggleexpreactawarding"), Summary("Toggles if users receive EXP.")]
            public async Task ToggleEXPAwardingReactions()
            {
                Configuration.UpdateConfiguration(awardingEXPReactionEnabled: !Configuration.Load().AwardingEXPReactionEnabled);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("EXP awarding via reactions has been toggled by " + Context.User.Mention + " (enabled: " + Configuration.Load().AwardingEXPReactionEnabled.ToYesNo() + ")");
            }

            [Command("toggleexpreactpostawarding"), Summary("Toggles if users receive EXP.")]
            public async Task ToggleEXPAwardingReactionsPoster()
            {
                Configuration.UpdateConfiguration(awardingEXPReactPostEnabled: !Configuration.Load().AwardingEXPReactPostEnabled);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("EXP awarding has been toggled by " + Context.User.Mention + " (enabled: " + Configuration.Load().AwardingEXPReactPostEnabled.ToYesNo() + ")");
            }

            [Command("toggleawardingexpmention"), Summary("Toggles if users get mentioned when they level up.")]
            public async Task ToggleAwardingEXPMention()
            {
                Configuration.UpdateConfiguration(awardingEXPMentionUser: !Configuration.Load().AwardingEXPMentionUser);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("AwardingEXPMentionUser has been toggled by " + Context.User.Mention + " (enabled: " + Configuration.Load().AwardingEXPMentionUser.ToYesNo() + ")");
            }
            
            [Command("leaderboardtrophyurl"), Summary("")]
            public async Task SetLeaderboardTrophyUrl(string link)
            {
                string oldValue = Configuration.Load().LeaderboardTrophyUrl;
                Configuration.UpdateConfiguration(leaderboardTrophyUrl: link);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated the Leaderboard Trophy URL to: " + link + " (was: " + oldValue + ")");
            }

            [Command("leaderboardembedcolor"), Summary("")]
            public async Task SetLeaderboardEmbedColor(uint colorId)
            {
                uint oldValue = Configuration.Load().LeaderboardEmbedColor;
                Configuration.UpdateConfiguration(leaderboardEmbedColor: colorId);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated the Leaderboard Embed Color ID to: " + colorId + " (was: " + oldValue + ")");
            }

            [Command("toggleshowallawards"), Summary("Toggles if users receive EXP.")]
            public async Task ToggleShowingAllAwards()
            {
                Configuration.UpdateConfiguration(showAllAwards: !Configuration.Load().ShowAllAwards);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("Showing All Awards has been toggled by " + Context.User.Mention + " (enabled: " + Configuration.Load().ShowAllAwards.ToYesNo() + ")");
            }

            [Command("awardsiconurl"), Summary("")]
            public async Task SetAwardsIconUrl(string link)
            {
                string oldValue = Configuration.Load().AwardsIconUrl;
                Configuration.UpdateConfiguration(awardsIconUrl: link);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(Context.User.Mention + " has updated the Awards Icon URL to: " + link + " (was: " + oldValue + ")");
            }
        }

        [Group("editdatabase")]
        public class DatabaseConfiguration : ModuleBase
        {
            [Command("")]
            public async Task SendSyntax()
            {
                await ReplyAsync("**Syntax:** " +
                                 Guild.Load(Context.Guild.Id).Prefix + "editdatabase [variable] [command syntax]\n```" +
                                 "Available Commands\n" +
                                 "-----------------------------\n" +
                                 "-> editdatabase host [host address]\n" +
                                 "-> editdatabase port [port number]\n" +
                                 "-> editdatabase user [username]\n" +
                                 "-> editdatabase password [password]\n" +
                                 "-> editdatabase name [database name]\n" +
                                 "```");
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
            }

            [Command("host")]
            public async Task SetDatabaseAddress([Remainder] string value)
            {
                Configuration.UpdateConfiguration(databaseHost: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("Database Hostname has been changed by " + Context.User.Mention + " to " + value);
            }

            [Command("port")]
            public async Task SetDatabasePort([Remainder] int value)
            {
                Configuration.UpdateConfiguration(databasePort: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("Database Port has been changed by " + Context.User.Mention + " to " + value);
            }

            [Command("user")]
            public async Task SetDatabaseUser([Remainder] string value)
            {
                Configuration.UpdateConfiguration(databaseUser: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("Database User has been changed by " + Context.User.Mention + " to " + value);
            }

            [Command("password")]
            public async Task SetDatabasePassword([Remainder] string value)
            {
                Configuration.UpdateConfiguration(databasePassword: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("Database Password has been changed by " + Context.User.Mention + " to " + value);
            }

            [Command("name")]
            public async Task SetDatabaseName([Remainder] string value)
            {
                Configuration.UpdateConfiguration(databaseName: value);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("Database Name has been changed by " + Context.User.Mention + " to " + value);
            }
        }

        [Group("editstring")]
        public class StringsConfigurationModule : ModuleBase
        {
            [Command("")]
            public async Task SendSyntax()
            {
                await ReplyAsync("**Syntax:** " +
                                 Guild.Load(Context.Guild.Id).Prefix + "editstring [variable] [command syntax]\n```" +
                                 "Available Commands\n" +
                                 "-----------------------------\n" +
                                 "-> editstring DefaultWebsiteName [name]\n" +
                                 "```");
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
            }

            [Command("defaultwebsitename"), Summary("Sets the default name for users website.")]
            public async Task SetDefaultWebsiteName([Remainder] string name = null)
            {
                StringConfiguration.UpdateConfiguration(websiteName: name);
                AdminLog.Log(Context.User.Id, Context.Message.Content, Context.Guild.Id);
                
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("DefaultWebsiteName has been changed by " + Context.User.Mention + " to " + name);
            }
        }
    }
}
