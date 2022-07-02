using Discord.InviteFilter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.InviteFilter.Models
{
    public class GuildInviteSettings
    {
        public ulong GuildId { get; set; }
        public PunishAction PunishAction { get; set; }
        public ulong? LogChannelId { get; set; }
        public long? TimeoutDurationInMinutes { get; set; }

        public GuildInviteSettings(ulong guildId, PunishAction punishAction, ulong? logChannelId, long? timeoutDurationInMinutes)
        {
            GuildId = guildId;
            PunishAction = punishAction;
            LogChannelId = logChannelId;
            TimeoutDurationInMinutes = timeoutDurationInMinutes;
        }
    }
}
