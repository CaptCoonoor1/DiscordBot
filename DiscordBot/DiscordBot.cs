﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net.Providers.UDPClient;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;

using DiscordBot.Common;
using DiscordBot.Database;
using DiscordBot.Extensions;
using DiscordBot.Handlers;
using DiscordBot.Modules.Mod;
using DiscordBot.Objects;
using MelissaNet;

namespace DiscordBot
{
    public class DiscordBot
    {
        public static DiscordSocketClient Bot;
	    public static CommandService _commandService;
	    public static IServiceProvider _serviceProvider;

        public async Task RunBotAsync()
        {
            Bot = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 1000,
                WebSocketProvider = WS4NetProvider.Instance,
                UdpSocketProvider = UDPClientProvider.Instance,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                AlwaysDownloadUsers = true,
                ConnectionTimeout = int.MaxValue,

            });
            _commandService = new CommandService();
            _serviceProvider = ConfigureServices();

            // Create Tasks for Bot Events
            #region Events
            Bot.Log += Log;
            
            Bot.UserJoined += UserHandler.UserJoined;
            Bot.UserLeft += UserHandler.UserLeft;
	        Bot.UserUpdated += UserHandler.UserUpdated;
            
            Bot.ChannelCreated += ChannelHandler.ChannelCreated;
            Bot.ChannelDestroyed += ChannelHandler.ChannelDestroyed;
            
            Bot.JoinedGuild += GuildHandler.JoinedGuild;
            
            Bot.ReactionAdded += ReactionHandler.ReactionAdded;
            
            Bot.MessageReceived += MessageReceived;
            
            Bot.Ready += Ready;
            
            Bot.Disconnected += Disconnected;
            #endregion
	        
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            await LoginAndStart();

            // Keep the program running.
            await Task.Delay(-1);
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(Bot)
                .AddSingleton<InteractiveService>();
            return services.BuildServiceProvider();
        }

        private static async Task LoginAndStart()
        {
            try
            {
                await Bot.LoginAsync(TokenType.Bot, Cryptography.DecryptString(Configuration.Load().BotToken));
                await Bot.StartAsync();
            }
            catch (CryptographicException exception)
            {
	            await new LogMessage(LogSeverity.Warning, "Startup", "Exception Caught: " + exception.ToString()).PrintToConsole();
	            await ReEnterToken();
            }
            catch (Discord.Net.HttpException exception)
            {
                if (exception.HttpCode == HttpStatusCode.Unauthorized || exception.HttpCode == HttpStatusCode.Forbidden)
                {
	                await ReEnterToken();
                }
            }
            catch (FormatException)
            {
	            await ReEnterToken();
            }
            catch (Exception)
            {
	            await new LogMessage(LogSeverity.Warning, "Startup", "An error has occured.").PrintToConsole();
                throw;
            }
        }

        private static async Task ReEnterToken(string reasoning = "The token stored on file doesn't seem to be working. Please re-enter the bot token.")
        {
	        await new LogMessage(LogSeverity.Warning, "Startup", reasoning).PrintToConsole();

	        await new LogMessage(LogSeverity.Info, "Startup", "Please enter the Bot Token:").PrintToConsole();
            Configuration.UpdateConfiguration(botToken:Cryptography.EncryptString(Console.ReadLine()));

	        await new LogMessage(LogSeverity.Warning, "Startup", "Token saved successfully. Console will now be cleared for security reasons. Press the 'enter' key to continue.").PrintToConsole();
            Console.ReadLine();
            Console.Clear();

            new DiscordBot().RunBotAsync().GetAwaiter().GetResult();
        }

        internal static Task Log(LogMessage logMessage)
        {
	        var cc = Console.ForegroundColor;
	        switch (logMessage.Severity)
	        {
		        case LogSeverity.Critical:
		        case LogSeverity.Error:
			        Console.ForegroundColor = ConsoleColor.Red;
			        break;

	            case LogSeverity.Warning:
			        Console.ForegroundColor = ConsoleColor.Yellow;
			        break;

	            case LogSeverity.Info:
			        Console.ForegroundColor = ConsoleColor.White;
			        break;

	            case LogSeverity.Verbose:
	            case LogSeverity.Debug:
			        Console.ForegroundColor = ConsoleColor.DarkGray;
			        break;

	            default:
			        Console.ForegroundColor = ConsoleColor.Blue;
			        break;
	        }
	        Console.WriteLine($@"{DateTime.Now,-19} [{logMessage.Severity,8}] {logMessage.Source}: {logMessage.Message}"); // .PrintToConsole uses this Console.WriteLine so do not change it!
            Console.ForegroundColor = cc;
	        return Task.CompletedTask;
        }

        private static async Task Ready()
        {
            List<Tuple<SocketGuildUser, SocketGuild>> offlineList = new List<Tuple<SocketGuildUser, SocketGuild>>();

            await Bot.SetGameAsync(Configuration.Load().StatusText, Configuration.Load().StatusLink,
                (ActivityType) Configuration.Load().StatusActivity);

			await Bot.SetStatusAsync(Configuration.Load().Status);

			ModeratorModule.ActiveForDateTime = DateTime.Now;

	        await new LogMessage(LogSeverity.Info, "Startup", "-----------------------------------------------------------------").PrintToConsole();
			foreach (SocketGuild g in Bot.Guilds)
			{
			    Console.ResetColor();
				await new LogMessage(LogSeverity.Info, "Startup", "Attempting to load " + g.Name).PrintToConsole();

				GuildConfiguration.EnsureExists(g.Id);

				//todo: maybe add something like this to add guild bans to a database to be read on a website?
//				if (g.GetUser(Bot.CurrentUser.Id).IsGuildAdministrator() || g.GetUser(Bot.CurrentUser.Id).GuildPermissions.BanMembers)
//				{
//					var bans = await g.GetBansAsync();
//					foreach (IBan b in bans)
//					{
//						await new LogMessage(LogSeverity.Info, g.Name, "@" + b.User.Username + " | Reason: " + b.Reason).PrintToConsole();
//					}
//					
//					await new LogMessage(LogSeverity.Info, "Guild Bans", "Updated Guild Bans Successfully.").PrintToConsole();
//				}
//				else
//				{
//					await new LogMessage(LogSeverity.Info, "Guild Bans", "Unable to get banned users - Bot doesn't have the required permission(s).").PrintToConsole();
//				}
				// end.

				await new LogMessage(LogSeverity.Info, "Startup", "-----------------------------------------------------------------").PrintToConsole();

				foreach (SocketGuildUser u in g.Users)
				{
					//Insert new users into the database by using INSERT IGNORE
					List<(string, string)> queryParams = new List<(string id, string value)>()
					{
						("@username", u.Username),
						("@avatarUrl", u.GetAvatarUrl())
					};
					
					int rowsUpdated = DatabaseActivity.ExecuteNonQueryCommand(
						"INSERT IGNORE INTO " +
						"users(id,username,avatarUrl) " +
						"VALUES (" + u.Id + ", @username, @avatarUrl);", queryParams);
					
					//end.
					
					if (rowsUpdated > 0) // If any rows were affected, add the user to the list to be dealt with later.
					{
						offlineList.Add(new Tuple<SocketGuildUser, SocketGuild>(u, g));
					}
				}

				await new LogMessage(LogSeverity.Info, "Startup", "-----------------------------------------------------------------").PrintToConsole();

			    foreach (SocketGuildChannel c in g.Channels)
			    {
			        Channel.EnsureExists(c.Id);
			    }

				await new LogMessage(LogSeverity.Info, "Startup", "-----------------------------------------------------------------").PrintToConsole();
            }
	        
            if (offlineList.Any())
            {
	            await new LogMessage(LogSeverity.Info, "Startup", offlineList.Count + " new users added.").PrintToConsole();
                foreach (Tuple<SocketGuildUser, SocketGuild> tupleList in offlineList)
                {
	                await new LogMessage(LogSeverity.Warning, "Startup", tupleList.Item1.Mention + " (" + tupleList.Item1.Id + ") joined " + tupleList.Item2.Name + " while the Bot was offline.").PrintToConsole();
                }
            }
            else
            {
	            await new LogMessage(LogSeverity.Info, "Startup", "No new users added.").PrintToConsole();
            }

	        await new LogMessage(LogSeverity.Info, "Startup", Bot.CurrentUser.Username + " loaded.").PrintToConsole();

			// Send message to log channel to announce bot is up and running.
			Version v = Assembly.GetExecutingAssembly().GetName().Version;
			EmbedBuilder eb = new EmbedBuilder()
					.WithTitle("Startup Notification")
					.WithColor(59, 212, 50)
					.WithThumbnailUrl(Bot.CurrentUser.GetAvatarUrl())
					.WithDescription("**" + Bot.CurrentUser.Username + "** : ready event executed.")
                    .AddField("Version", v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision, true)
                    .AddField("Latest Version", MelissaNet.Modules.Updater.CheckForNewVersion("MogiiBot3").Item1, true)
                    .AddField("MelissaNet", VersionInfo.Version, true)
					.AddField("Latency", Bot.Latency + "ms", true)
                    .WithCurrentTimestamp();
				await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("", false, eb.Build());

            if (offlineList.Any())
            {
                foreach (Tuple<SocketGuildUser, SocketGuild> tupleList in offlineList)
                {
                    await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("[ALERT] While " + Bot.CurrentUser.Username + " was offline, " + tupleList.Item1.Mention + " (" + tupleList.Item1.Id + ") joined " + tupleList.Item2.Name + ". They have been added to the database.");
                }
            }
        }

        private static async Task Disconnected(Exception exception)
        {
	        await new LogMessage(LogSeverity.Critical, "Disconnected", exception.ToString()).PrintToConsole();
        }
	    
        private static async Task MessageReceived(SocketMessage messageParam)
        {
	        Console.WriteLine("1");
            if (!(messageParam is SocketUserMessage message)) return; // If the message is null, return.
	        Console.WriteLine("1.1");
            if (message.Author.IsBot) return; // If the message was posted by a BOT account, return.
	        Console.WriteLine("1.2");
            if (message.Author.IsUserIgnoredByBot() && message.Author.Id != Configuration.Load().Developer) { return; } // If the bot is ignoring the user AND the user NOT Melissa.

	        Console.WriteLine("2");
            // If the message came from somewhere that is not a text channel -> Private Message
            if (!(messageParam.Channel is ITextChannel))
            {
                EmbedFooterBuilder efb = new EmbedFooterBuilder()
                    .WithText("UID: " + message.Author.Id + " | MID: " + message.Id);
                EmbedBuilder eb = new EmbedBuilder()
                    .WithTitle("Private Message - Posted By: @" + message.Author.Username + "#" + message.Author.Discriminator)
                    .WithDescription(message.Content)
                    .WithFooter(efb)
                    .WithCurrentTimestamp();

                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("", false, eb.Build());

                return;
            }

	        Console.WriteLine("3");
	        await new LogMessage(LogSeverity.Info, "MessageReceived", "[" + messageParam.Channel.GetGuild().Name + "/#" + messageParam.Channel.Name + "] " + "[@" + 
	                                                            messageParam.Author.Username + "] : " + messageParam.Content).PrintToConsole();

            var uPrefix = message.Author.GetCustomPrefix();
            var gPrefix = GuildConfiguration.Load(message.Channel.GetGuild().Id).Prefix;
            if (string.IsNullOrEmpty(uPrefix)) { uPrefix = gPrefix; } // Fixes an issue with users not receiving coins due to null prefix.
            var argPos = 0;
            if (message.HasStringPrefix(gPrefix, ref argPos) || 
                message.HasMentionPrefix(Bot.CurrentUser, ref argPos) || 
                message.HasStringPrefix(uPrefix, ref argPos)) {
                var context = new SocketCommandContext(Bot, message);
                var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

                if (!result.IsSuccess && Configuration.Load().UnknownCommandEnabled)
                {
                    var errorMessage = await context.Channel.SendMessageAsync(messageParam.Author.Mention + ", " + result.ErrorReason);

	                await new LogMessage(LogSeverity.Error, "MessageReceived", message.Author.Username + " - " + result.ErrorReason).PrintToConsole();

                    errorMessage.DeleteAfter(20);
                }
            }
            else if (message.Content.ToUpper() == "F") // If the message is just "F", pay respects.
            {
	            var respects = Configuration.Load().Respects + 1;
                Configuration.UpdateConfiguration(respects: respects);

                var eb = new EmbedBuilder()
                    .WithDescription("**" + message.Author.Username + "** has paid their respects.")
                    .WithFooter("Total Respects: " + respects)
                    .WithColor(message.Author.GetCustomRGB());

	            await message.Channel.SendMessageAsync("", false, eb.Build());
            }
            else
            {
	            if(Configuration.Load().AwardingEXPEnabled)
                {
	                if (message.Content.Length >= Configuration.Load().MinLengthForEXP)
	                {
		                if (Channel.Load(message.Channel.Id).AwardingEXP)
		                {
			                message.Author.AwardEXPToUser(message.Channel.GetGuild());
		                }
	                }
                }
            }
        }
    }
}
