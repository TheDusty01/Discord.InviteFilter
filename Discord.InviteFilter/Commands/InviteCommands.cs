using Discord.InviteFilter.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.InviteFilter.Commands
{
    [SlashCommandGroup("invfilter", "Configure the invite filter")]
    public class InviteCommands : ApplicationCommandModule
    {
        private readonly InviteService inviteService;

        public InviteCommands(InviteService inviteService)
        {
            this.inviteService = inviteService;
        }

        [SlashCommand("setup", "Setup the invite filter")]
        [SlashRequireGuild]
        [SlashRequireBotPermissions(Permissions.EmbedLinks | Permissions.SendMessages)]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task SetupCommand(InteractionContext ctx,
            [Option("punishment", "What should happen when someone posts an external invite link")] PunishAction action,
            [Option("log_channel", "The channel where punishments should be logged to")] DiscordChannel? logChannel = null,
            [Option("timeout_duration", "The timeout duration (in minutes) after a user sent an external invite")] long? durationInMinutes = null)
        {
            await ctx.DeferAsync(false);

            try
            {
                await inviteService.Setup(ctx.Guild, action, logChannel, durationInMinutes);
            }
            catch (TimeoutDurationNullException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"You must specify a ``timeout_duration`` when using **{action.GetName()}**."));
                return;
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong, please try again later."));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"Successfully setup invite filters for **{action.GetName()}**.\n\n" +
                "Make sure the bot has the following permissions:\n" +
                "**Send Messages**, **Manage Messages**, **Embed Links**, **Manage Server** and **Manage Channels**.\nFor auto bans **Ban Members** and auto timeouts the **Moderate Members** permissions.\n\n" +
                "To disable the bot for a specific channel just remove the **View Channel** permission on that channel for the bot."));          
        }
    }

}
