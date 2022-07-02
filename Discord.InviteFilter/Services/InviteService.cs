using Discord.InviteFilter.Commands;
using Discord.InviteFilter.Data;
using Discord.InviteFilter.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.InviteFilter.Services
{
    public class InviteService
    {
        private const string DiscordLinkWildcard = "discord.gg/";
        private readonly ILogger<InviteService> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbFactory;

        public InviteService(ILogger<InviteService> logger, IDbContextFactory<ApplicationDbContext> dbFactory, DiscordClient discordClient)
        {
            this.logger = logger;
            this.dbFactory = dbFactory;
            discordClient.MessageCreated += DiscordClient_MessageCreated;    
        }

        #region Configuration
        public async Task Setup(DiscordGuild guild, PunishAction action, DiscordChannel? logChannel = null, long? durationInMinutes = null)
        {
            if (action.HasFlag(PunishAction.Timeout) && durationInMinutes is null)
                throw new TimeoutDurationNullException();

            try
            {
                using (ApplicationDbContext dbCtx = dbFactory.CreateDbContext())
                {
                    GuildInviteSettings? guildInviteSettings = dbCtx.GuildInviteSettings.Find(guild.Id);
                    if (guildInviteSettings is null)
                    {
                        dbCtx.GuildInviteSettings.Add(new GuildInviteSettings(guild.Id, action, logChannel?.Id, durationInMinutes));
                    }
                    else
                    {
                        guildInviteSettings.PunishAction = action;
                        guildInviteSettings.LogChannelId = logChannel?.Id;
                        guildInviteSettings.TimeoutDurationInMinutes = durationInMinutes;
                    }

                    await dbCtx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Couldn't save settings for guild '{guildName}' ({guildId})", guild.Name, guild.Id);
                throw;
            }
        }
        #endregion

        #region Logic
        private Task DiscordClient_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsCurrent || e.Channel.IsPrivate)
                return Task.CompletedTask;

            string content = e.Message.Content;
            int inviteLinkStart = content.IndexOf(DiscordLinkWildcard, StringComparison.OrdinalIgnoreCase);
            if (inviteLinkStart < 0)
                return Task.CompletedTask;

            string code = content.Substring(inviteLinkStart + DiscordLinkWildcard.Length);
            int spaceIndex = code.IndexOf(" ");
            if (spaceIndex > -1)
            {
                code = code.Substring(0, spaceIndex);
            }

            var guild = e.Guild;
            if (code == guild.VanityUrlCode)
                return Task.CompletedTask;

            _ = Task.Run(async () =>
            {
                var channel = e.Channel;

                IReadOnlyList<DiscordInvite> invites;
                try
                {
                    invites = await guild.GetInvitesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Couldn't retrieve invites for guild '{guildName}' ({guildId})",
                        guild.Name, guild.Id);
                    return;
                }

                DiscordInvite? invite = invites.Where(inv => inv.Code == code).FirstOrDefault();
                if (invite is not null)
                    return;

                GuildInviteSettings? settings;
                try
                {
                    using (ApplicationDbContext dbCtx = dbFactory.CreateDbContext())
                    {
                        settings = dbCtx.GuildInviteSettings.Find(guild.Id);
                        if (settings is null || settings.PunishAction == PunishAction.Disabled)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Couldn't retrieve GuildInviteSettings for guild '{guildName}' ({guildId})", guild.Name, guild.Id);
                    return;
                }

                string invLink = DiscordLinkWildcard + code;

                DiscordChannel? logChannel = null;
                if (settings.LogChannelId is not null)
                {
                    logChannel = guild.GetChannel(settings.LogChannelId.Value);
                }

                List<Action> logActions = new List<Action>();

                DiscordMember member = (DiscordMember)e.Author;
                if (settings.PunishAction.HasFlag(PunishAction.Delete))
                {
                    try
                    {
                        await e.Message.DeleteAsync("Invite link to other server: " + invLink);
                        logger.LogInformation("Deleted message '{invite}' ({msgId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            invLink,
                            e.Message.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

                        logActions.Add(async () =>
                        {
                            await LogActionAsync(PunishAction.Delete, logChannel!, member, e.Message, null);
                        });
                    }
                    catch (UnauthorizedException ex)
                    {
                        logger.LogInformation(ex, "Couldn't delete message ({msgId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            e.Message.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

                        logActions.Add(async () =>
                        {
                            await LogActionNoPermissionAsync(settings.PunishAction, logChannel!, member, e.Message);
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation(ex, "Couldn't delete message ({msgId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            e.Message.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);
                    }
                }

                if (settings.PunishAction.HasFlag(PunishAction.Timeout))
                {
                    try
                    {
                        if (settings.TimeoutDurationInMinutes is null)
                            return;

                        await member.TimeoutAsync(DateTimeOffset.UtcNow.AddMinutes(Convert.ToDouble(settings!.TimeoutDurationInMinutes)), "Invite link to other server: " + invLink);
                        logger.LogInformation("Timeouted member '{memberName}' ({memberId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            member.Username, member.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

                        logActions.Add(async () =>
                        {
                            await LogActionAsync(PunishAction.Timeout, logChannel!, member, null, settings!.TimeoutDurationInMinutes);
                        });
                    }
                    catch (UnauthorizedException ex)
                    {
                        logger.LogInformation(ex, "Couldn't timeout member '{memberName}' ({memberId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            member.Username, member.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

                        logActions.Add(async () =>
                        {
                            await LogActionNoPermissionAsync(settings.PunishAction, logChannel!, member, null);
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation(ex, "Couldn't timeout member '{memberName}' ({memberId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            member.Username, member.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);
                    }
                }
                else if (settings.PunishAction.HasFlag(PunishAction.Ban))
                {
                    try
                    {
                        await member.BanAsync(reason: "Invite link to other server: " + invLink);
                        logger.LogInformation("Banned member '{memberName}' ({memberId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            member.Username, member.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

                        logActions.Add(async () =>
                        {
                            await LogActionAsync(PunishAction.Ban, logChannel!, member, null, null);
                        });
                    }
                    catch (UnauthorizedException ex)
                    {
                        logger.LogInformation(ex, "Couldn't ban member '{memberName}' ({memberId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            member.Username, member.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

                        logActions.Add(async () =>
                        {
                            await LogActionNoPermissionAsync(settings.PunishAction, logChannel!, member, null);
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation(ex, "Couldn't ban member '{memberName}' ({memberId}) in channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                            member.Username, member.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);
                    }
                }

                if (logChannel is not null)
                {
                    foreach (var action in logActions)
                    {
                        action();
                        await Task.Delay(1000);
                    }
                }

            });

            return Task.CompletedTask;
        }

        private async Task LogActionAsync(PunishAction action, DiscordChannel logChannel, DiscordMember member, DiscordMessage? messageOfUser, long? timeoutInMinutes)
        {
            DiscordEmbed embed;
            if (action.HasFlag(PunishAction.Delete) && messageOfUser is not null)
            {
                embed = GenerateEmbed(member, DiscordColor.Orange,
                    $"**Message sent by {member.Mention} deleted in {messageOfUser.Channel.Mention}**\n{messageOfUser.Content}",
                    $"User Id: {member.Id} | Message Id: {messageOfUser.Id}");
            }
            else if (action.HasFlag(PunishAction.Timeout) && timeoutInMinutes is not null)
            {
                embed = GenerateEmbed(member, DiscordColor.IndianRed,
                    $"**Member {member.Mention} timeouted for {TimeSpan.FromMinutes(timeoutInMinutes.Value)}**",
                    $"User Id: {member.Id}");
            }
            else if (action.HasFlag(PunishAction.Ban))
            {
                embed = GenerateEmbed(member, new DiscordColor("#330000"),
                    $"**Member {member.Mention} banned**",
                    $"User Id: {member.Id}");
            }
            else
            {
                embed = GenerateEmbed(member, DiscordColor.Blurple,
                    "Unknown",
                    $"User Id: {member.Id}");
            }

            try
            {
                await logChannel.SendMessageAsync(embed);
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "Couldn't send log message to channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                    logChannel.Name, logChannel.Id, logChannel.Guild.Name, logChannel.Guild.Id);
            }
        }

        private async Task LogActionNoPermissionAsync(PunishAction action, DiscordChannel logChannel, DiscordMember member, DiscordMessage? messageOfUser)
        {
            DiscordEmbed embed;
            DiscordColor color = DiscordColor.IndianRed;
            if (action.HasFlag(PunishAction.Delete) && messageOfUser is not null)
            {
                embed = GenerateEmbed(member, color,
                    $"**I am not allowed to delete the message sent by {member.Mention} in {messageOfUser.Channel.Mention} even though you set the bot to ``{action.GetName()}``**\n{messageOfUser.Content}",
                    $"User Id: {member.Id} | Message Id: {messageOfUser.Id}");
            }
            else if (action.HasFlag(PunishAction.Timeout))
            {
                embed = GenerateEmbed(member, color,
                    $"**I am not allowed to timeout member {member.Mention} even though you set the bot to ``{action.GetName()}``**",
                    $"User Id: {member.Id}");
            }
            else if (action.HasFlag(PunishAction.Ban))
            {
                embed = GenerateEmbed(member, color,
                    $"**I am not allowed to ban member {member.Mention} even though you set the bot to ``{action.GetName()}``**",
                    $"User Id: {member.Id}");
            }
            else
            {
                embed = GenerateEmbed(member, DiscordColor.Blurple,
                    "Unknown",
                    $"User Id: {member.Id}");
            }

            try
            {
                await logChannel.SendMessageAsync(embed);
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "Couldn't send no permission log message to channel '{channelName}' ({channelId}) for guild '{guildName}' ({guildId})",
                    logChannel.Name, logChannel.Id, logChannel.Guild.Name, logChannel.Guild.Id);
            }
        }

        private static DiscordEmbed GenerateEmbed(DiscordMember member, DiscordColor color, string description, string footer)
        {
            var builder = new DiscordEmbedBuilder()
                .WithAuthor($"{member.Username}#{member.Discriminator}", null, member.AvatarUrl)
                .WithDescription(description)
                .WithFooter(footer)
                .WithColor(color)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithThumbnail(member.AvatarUrl, 128, 128);

            return builder.Build();
        }
        #endregion
    }

    public class TimeoutDurationNullException : Exception
    {
        public TimeoutDurationNullException() : base("Duration in minutes in null")
        {

        }
    }

    public enum PunishAction
    {
        [ChoiceName("Disabled")]
        Disabled = 0,

        [ChoiceName("Delete message")]
        Delete = 1,
        [ChoiceName("Timeout the user")]
        Timeout = 2,

        [ChoiceName("Delete and timeout the user")]
        DeleteAndTimeout = Delete | Timeout,

        [ChoiceName("Ban the user")]
        Ban = 4,
        [ChoiceName("Delete and ban the user")]
        DeleteAndBan = Delete | Ban
    }
}
