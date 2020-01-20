﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Common;
using DiscordBot.Database;
using DiscordBot.Handlers;
using DiscordBot.Extensions;
using DiscordBot.Objects;

namespace DiscordBot.Extensions
{
    public static class UserExtensions
    {
        public static int GetLevel(this IUser user)
        {
            return User.Load(user.Id).Level;
        }

        public static int GetEXP(this IUser user)
        {
            return User.Load(user.Id).EXP;
        }
        
        public static string GetName(this IUser user)
        {
            return User.Load(user.Id).Name;
        }
        public static string GetGender(this IUser user)
        {
            return User.Load(user.Id).Gender;
        }
        public static string GetPronouns(this IUser user)
        {
            return User.Load(user.Id).Pronouns;
        }
        public static string GetAbout(this IUser user)
        {
            return User.Load(user.Id).About;
        }
        public static string GetCustomPrefix(this IUser user)
        {
            return User.Load(user.Id).CustomPrefix;
        }
        public static Color GetCustomRGB(this IUser user)
        {
            return new Color(User.Load(user.Id).AboutR, User.Load(user.Id).AboutG, User.Load(user.Id).AboutB);
        }
        public static string GetMinecraftUsername(this IUser user)
        {
            return User.Load(user.Id).MinecraftUsername;
        }
        public static string GetGitHubUsername(this IUser user)
        {
            return User.Load(user.Id).GitHubUsername;
        }
        public static string GetInstagramUsername(this IUser user)
        {
            return User.Load(user.Id).InstagramUsername;
        }
        public static string GetSnapchatUsername(this IUser user)
        {
            return User.Load(user.Id).SnapchatUsername;
        }
        public static string GetPokemonGoFriendCode(this IUser user)
        {
            return User.Load(user.Id).PokemonGoFriendCode;
        }
        public static string GetFooterText(this IUser user)
        {
            return User.Load(user.Id).FooterText;
        }
        public static string GetWebsiteName(this IUser user)
        {
            return User.Load(user.Id).WebsiteName;
        }
        public static string GetWebsiteUrl(this IUser user)
        {
            return User.Load(user.Id).WebsiteUrl;
        }
        
        public static string GetEmbedAuthorBuilderIconUrl(this IUser user)
        {
            return User.Load(user.Id).EmbedAuthorBuilderIconUrl;
        }
        public static string GetEmbedFooterBuilderIconUrl(this IUser user)
        {
            return User.Load(user.Id).EmbedFooterBuilderIconUrl;
        }
        public static bool IsUserIgnoredByBot(this IUser user)
        {
            return User.Load(user.Id).IsBotIgnoringUser;
        }

        public static async void AwardEXPToUser(this IUser user, SocketGuild guild, int exp = 1)
        {
            if (user.IsBot)
            {
                return;
            }
            
            try
            {
                int updatedEXP = user.GetEXP() + exp;
                
                List<(string, string)> queryParams = new List<(string, string)>()
                {
                    ("@exp", updatedEXP.ToString())
                };
                
                DatabaseActivity.ExecuteNonQueryCommand("UPDATE users SET exp=@exp WHERE id='" + user.Id + "';", queryParams);
                
                await user.AttemptLevelUp(guild);
            }
            catch (Exception e)
            {
                await new LogMessage(LogSeverity.Warning, "UserExtensions", e.Message).PrintToConsole();
            }
        }
        
        public static double EXPToLevelUp(this IUser user, int? level = null)
        {
            int userLevel = level ?? (user.GetLevel() + 1);
            return (0.04 * (Math.Pow(userLevel, 3))) + (0.8 * (Math.Pow(userLevel, 2))) + (2 * userLevel);   
        }
        public static async Task AttemptLevelUp(this IUser user, SocketGuild guild)
        {
            double requiredEXP = user.EXPToLevelUp();
            
            if (user.GetEXP() >= Math.Round(requiredEXP))
            {
                try
                {
                    int updatedLevel = user.GetLevel() + 1;
                
                    List<(string, string)> queryParams = new List<(string, string)>()
                    {
                        ("@level", updatedLevel.ToString())
                    };
                
                    DatabaseActivity.ExecuteNonQueryCommand("UPDATE users SET level=@level WHERE id='" + user.Id + "';", queryParams);
                    
                    SocketTextChannel botChannel = Guild.Load(guild.Id).BotChannelID.GetTextChannel() ??
                                                   Guild.Load(guild.Id).WelcomeChannelID.GetTextChannel();
                    
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = "Level Up!",
                        Color = user.GetCustomRGB()
                    }.WithCurrentTimestamp();

                    if (Configuration.Load().AwardingEXPMentionUser)
                    {
                        eb.WithDescription("Well done " + user.Mention + "! You levelled up to level " +
                                           user.GetLevel() + "! Gain " +
                                           (Math.Round(EXPToLevelUp(user)) - user.GetEXP()) +
                                           " more EXP to level up again!");
                    }
                    else
                    {
                        eb.WithDescription("Well done " + user.Username + "! You levelled up to level " +
                                           user.GetLevel() + "! Gain " +
                                           (Math.Round(EXPToLevelUp(user)) - user.GetEXP()) +
                                           " more EXP to level up again!");
                    }
                    
                    await botChannel.SendMessageAsync("", false, eb.Build());
                }
                catch (Exception e)
                {
                    await new LogMessage(LogSeverity.Warning, "UserExtensions", e.Message).PrintToConsole();
                }
            }
        }
    }
}
