﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using DiscordBot.Common;
using DiscordBot.Database;
using DiscordBot.Extensions;
using DiscordBot.Objects;
using MelissaNet;

namespace DiscordBot.Handlers
{
    public class UserHandler
    {
        public static async Task UserJoined(SocketGuildUser e)
        {
            //Insert new users into the database by using INSERT IGNORE
            List<(string, string)> queryParams = new List<(string id, string value)>()
            {
                ("@username", e.Username),
                ("@avatarUrl", e.GetAvatarUrl())
            };
					
            int rowsUpdated = DatabaseActivity.ExecuteNonQueryCommand(
                "INSERT IGNORE INTO " +
                "users(id,username,avatarUrl) " +
                "VALUES (" + e.Id + ", @username, @avatarUrl);", queryParams);
					
            //end.
            
            if (rowsUpdated == 0)
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Title = e.Guild.Name + " - User Joined - " + e.Username,
                    Description = "@" + e.Username + "\n" + e.Id,
                    Color = new Color(28, 255, 28),
                    ThumbnailUrl = e.GetAvatarUrl()
                }.WithCurrentTimestamp();

                if (GuildConfiguration.Load(e.Guild.Id).WelcomeMessage != null && GuildConfiguration.Load(e.Guild.Id).WelcomeChannelId != 0)
                    await GuildConfiguration.Load(e.Guild.Id).WelcomeChannelId.GetTextChannel().SendMessageAsync(GuildConfiguration.Load(e.Guild.Id).WelcomeMessage.ModifyStringFlags(e));

                await GuildConfiguration.Load(e.Guild.Id).LogChannelId.GetTextChannel().SendMessageAsync("", false, eb.Build());
                
                EmbedBuilder lEB = new EmbedBuilder()
                {
                    Title = "New User - " + e.Username,
                    Description = e.Id + ".json created successfully.",
                    Color = new Color(28, 255, 28),
                    ThumbnailUrl = e.GetAvatarUrl()
                }.WithCurrentTimestamp();
                
                //await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync(e.Username + " was successfully added to the database. [" + e.Id + "]");
                await Configuration.Load().LogChannelId.GetTextChannel().SendMessageAsync("", false, lEB.Build());
            }
            else
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Title = e.Guild.Name + " - User Joined - " + e.Username,
                    Description = "",
                    ThumbnailUrl = e.GetAvatarUrl(),
                    Color = new Color(255, 28, 28),
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = "ID: " + e.Id + " • Team Member: " + e.IsTeamMember().ToYesNo()
                    }
                }.WithCurrentTimestamp();

                if (e.GetAbout() != null) eb.AddField("About " + e.Username, e.GetAbout());

                eb.AddField("Username", "@" + e.Username + "#" + e.DiscriminatorValue, true);
                eb.AddField("Level", e.GetLevel(), true);
                eb.AddField("EXP", e.GetEXP(), true);
                eb.AddField("Account Created", e.UserCreateDate(), true);
                eb.AddField("Joined Guild", e.GuildJoinDate(), true);

                if (!String.IsNullOrEmpty(e.GetName())) eb.AddField("Name", e.GetName(), true);
                if (!String.IsNullOrEmpty(e.GetGender())) eb.AddField("Gender", e.GetGender(), true);
                if (!String.IsNullOrEmpty(e.GetPronouns())) eb.AddField("Pronouns", e.GetPronouns(), true);
                if (!String.IsNullOrEmpty(e.GetMinecraftUsername())) eb.AddField("Minecraft Username", e.GetMinecraftUsername(), true);
                if (!String.IsNullOrEmpty(e.GetInstagramUsername())) eb.AddField("Instagram", "[" + e.GetInstagramUsername() + "](https://www.instagram.com/" + e.GetInstagramUsername() + "/)", true);
                if (!String.IsNullOrEmpty(e.GetSnapchatUsername())) eb.AddField("Snapchat", "[" + e.GetSnapchatUsername() + "](https://www.snapchat.com/add/" + e.GetSnapchatUsername() + "/)", true);
                if (!String.IsNullOrEmpty(e.GetGitHubUsername())) eb.AddField("GitHub", "[" + e.GetGitHubUsername() + "](https://github.com/" + e.GetGitHubUsername() + "/)", true);
                if (!String.IsNullOrEmpty(e.GetFooterText())) eb.AddField("Footer Text", e.GetFooterText(), true);

                await GuildConfiguration.Load(e.Guild.Id).LogChannelId.GetTextChannel().SendMessageAsync("", false, eb.Build());
            }
        }

        public static async Task UserLeft(SocketGuildUser e)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Description = "",
                ThumbnailUrl = e.GetAvatarUrl(),
                Color = new Color(255, 28, 28),
                Footer = new EmbedFooterBuilder()
                {
                    Text = "ID: " + e.Id + " • Team Member: " + e.IsTeamMember().ToYesNo()
                }
            }.WithCurrentTimestamp();

            if (e.Nickname != null) eb.WithTitle(e.Guild.Name + " - User Left - " + e.Nickname);
            else eb.WithTitle(e.Guild.Name + " - User Left - " + e.Username);

            if (e.GetAbout() != null) eb.AddField("About " + e.Username, e.GetAbout());

            eb.AddField("Username", "@" + e.Username + "#" + e.DiscriminatorValue, true);
            eb.AddField("Level", e.GetLevel(), true);
            eb.AddField("EXP", e.GetEXP(), true);
            eb.AddField("Account Created", e.UserCreateDate(), true);
            eb.AddField("Joined Guild", e.GuildJoinDate(), true);

            if (e.GetName() != null) eb.AddField("Name", e.GetName(), true);
            if (e.GetGender() != null) eb.AddField("Gender", e.GetGender(), true);
            if (e.GetPronouns() != null) eb.AddField("Pronouns", e.GetPronouns(), true);
            if (e.GetMinecraftUsername() != null) eb.AddField("Minecraft Username", e.GetMinecraftUsername(), true);
            if (e.GetSnapchatUsername() != null) eb.AddField("Snapchat", "[" + e.GetSnapchatUsername() + "](https://www.snapchat.com/add/" + e.GetSnapchatUsername() + "/)", true);
            if (e.GetFooterText() != null) eb.AddField("Footer Text", e.GetFooterText(), true);

            await GuildConfiguration.Load(e.Guild.Id).LogChannelId.GetTextChannel().SendMessageAsync("", false, eb.Build());
        }

        public static async Task UserUpdated(SocketUser cachedUser, SocketUser user)
        {
            if (user.Username != cachedUser.Username || user.GetAvatarUrl() != cachedUser.GetAvatarUrl()) // If user has updated username or avatar
            {
                List<(string, string)> queryParams = new List<(string, string)>()
                {
                    ("@username", user.Username),
                    ("@avatarUrl", user.GetAvatarUrl())
                };
                DatabaseActivity.ExecuteNonQueryCommand("UPDATE users SET username=@username, avatarUrl=@avatarUrl WHERE id='" + user.Id + "';", queryParams);

            }
        }
    }
}
