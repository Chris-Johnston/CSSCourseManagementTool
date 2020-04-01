using Discord.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Controllers
{
    public class AdminController : DiscordControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        public AdminController(ILogger<AdminController> logger, DiscordRestClient discordRestClient, IConfiguration config) : base(discordRestClient, config)
        {
            _logger = logger;
        }

        // this should be for misc stuff
    }
}
