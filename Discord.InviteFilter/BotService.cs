using Discord.InviteFilter.Commands;
using Discord.InviteFilter.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Discord.InviteFilter
{
    internal class BotService : IHostedService
    {
        private readonly ILogger<BotService> logger;
        private readonly IServiceProvider services;
        private readonly DiscordClient discordClient;
        private readonly SlashCommandsExtension slash;

        public static bool IsReady { get; private set; } = false;

        #region Init
        public BotService(ILogger<BotService> logger, IServiceProvider services, DiscordClient discordClient, SlashCommandsExtension slash)
        {
            this.logger = logger;
            this.services = services;
            this.discordClient = discordClient;
            this.slash = slash;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            ConfigureSlashCommands();
            //discordClient.GuildAvailable += DiscordClient_GuildAvailable;
            discordClient.GuildDownloadCompleted += DiscordClient_GuildDownloadCompleted;
            await discordClient.ConnectAsync(new DiscordActivity("Deleting invites..", ActivityType.Playing), UserStatus.Online).ConfigureAwait(false);
            await discordClient.InitializeAsync().ConfigureAwait(false);
        }

        //private Task DiscordClient_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        //{
        //    _ = Task.Run(async () =>
        //    {
        //        try
        //        {
        //            var invites = await e.Guild.GetInvitesAsync();
        //            logger.LogInformation("Successfully fetched invites for guild '{guildName}' ({guildId})\n{invites}", e.Guild.Name, e.Guild.Id, JsonConvert.SerializeObject(invites, Formatting.Indented));
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError(ex, "Couldn't fetch invites for guild '{guildName}' ({guildId})", e.Guild.Name, e.Guild.Id);
        //        }
        //    });

        //    return Task.CompletedTask;
        //}

        private Task DiscordClient_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            IsReady = true;
            logger.LogInformation("Bot started.");

            _ = Task.Run(InitServices);

            return Task.CompletedTask;
        }

        private void ConfigureSlashCommands()
        {
            slash.RegisterCommands<InviteCommands>();   // Global

            slash.SlashCommandErrored += async (s, e) =>
            {
                if (e.Exception is SlashExecutionChecksFailedException slex)
                {
                    foreach (var check in slex.FailedChecks)
                    {
                        switch (check)
                        {
                            case SlashRequireUserPermissionsAttribute:
                                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                    new DiscordInteractionResponseBuilder()
                                    .WithContent($"You don't have the needed permissions to execute this command. Please make sure you have the **Manage Server** permissions and try again.")
                                    .AsEphemeral(true));
                                return;

                            case SlashRequireBotPermissionsAttribute:
                                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                    new DiscordInteractionResponseBuilder().WithContent(
                                        $"I am not allowed to do this. To function properly I need the follwoing permissions:\n" +
                                        "**Send Messages**, **Manage Messages**, **Embed Links**, **Manage Server** and **Manage Channels**.\nFor auto bans **Ban Members** and auto timeouts the **Moderate Members** permissions.")
                                    .AsEphemeral(true));
                                return;
                        }
                    }
                }
            };
        }

        private void InitServices()
        {
            services.GetRequiredService<InviteService>();
        }
        #endregion

        public async Task StopAsync(CancellationToken stoppingToken)
        {
            IsReady = false;
            await discordClient.DisconnectAsync().ConfigureAwait(false);
        }

        public virtual void Dispose()
        {
            discordClient.Dispose();
        }
    }
}
