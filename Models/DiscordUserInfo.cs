using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Models
{
    public class DiscordUserInfo
    {
        public RestSelfUser CurrentUser { get; set; } = null;
        public RestUserGuild Guild { get; set; } = null;

        public bool InGuild
            => Guild != null;

        public bool IsMuted
            => Guild?.Permissions.SendMessages != true;

        public bool IsAdmin
            => Guild?.Permissions.Administrator == true;
    }
}
