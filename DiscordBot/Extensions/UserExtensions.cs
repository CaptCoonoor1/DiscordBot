﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Common;
using DiscordBot.Handlers;

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
            return User.Load(user.Id).Snapchat;
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

        public static void AwardEXPToUser(this IUser user, SocketGuild guild, int? exp = 1)
        {
            try
            {
                User.UpdateUser(user.Id, exp:(user.GetEXP() + exp));
                user.AttemptLevelUp(guild);
            }
            catch (Exception e)
            {
                ConsoleHandler.PrintExceptionToLog("UserExtensions", e);
            }
        }
        
        public static double EXPToLevelUp(this IUser user, int? level = null)
        {
            int userLevel = level ?? (user.GetLevel() + 1);
            return (0.04 * (Math.Pow(userLevel, 3))) + (0.8 * (Math.Pow(userLevel, 2))) + (2 * userLevel);   
        }
        public static void AttemptLevelUp(this IUser user, SocketGuild guild)
        {
            double requiredEXP = user.EXPToLevelUp();

            //debugging
//            new LogMessage(LogSeverity.Debug, "EXPToLevelUp", user.Username + " - (User EXP) - " + user.GetEXP()).PrintToConsole();
//            new LogMessage(LogSeverity.Debug, "EXPToLevelUp", user.Username + " - (Required EXP) " + requiredEXP).PrintToConsole();
//            new LogMessage(LogSeverity.Debug, "EXPToLevelUp", user.Username + " - (int Casting of Required EXP) " + (int)requiredEXP).PrintToConsole();
//            new LogMessage(LogSeverity.Debug, "EXPToLevelUp", user.Username + " - (Math Rounding of Required EXP) " + Math.Round(requiredEXP)).PrintToConsole();
            
            if (user.GetEXP() >= Math.Round(requiredEXP))
            {
                try
                {
                    User.UpdateUser(user.Id, level: (user.GetLevel() + 1));

                    SocketTextChannel botChannel = GuildConfiguration.Load(guild.Id).BotChannelId.GetTextChannel() ??
                                                   GuildConfiguration.Load(guild.Id).WelcomeChannelId.GetTextChannel();
                    
                    //botChannel.SendMessageAsync(user.Mention + " has leveled up to " + User.Load(user.Id).Level);
                    
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = "Level Up!",
                        Color = user.GetCustomRGB(),
                        Description = "Well done " + user.Mention + "! You levelled up to level " + user.GetLevel() + "! Gain " + (Math.Round(EXPToLevelUp(user)) - user.GetEXP()) + " more EXP to level up again!",
                    }.WithCurrentTimestamp();
                    
                    botChannel.SendMessageAsync("", false, eb.Build());
                }
                catch (Exception e)
                {
                    ConsoleHandler.PrintExceptionToLog("UserExtensions", e);
                }
            }
        }
    }
}
