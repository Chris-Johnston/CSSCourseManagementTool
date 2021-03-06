﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CSSCourseManagementWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Discord.Rest;
using Discord;
using Microsoft.Extensions.Configuration;

namespace CSSCourseManagementWeb.Controllers
{
    public class HomeController : DiscordControllerBase
    {
        private readonly ILogger<HomeController> _logger;

        
        public HomeController(ILogger<HomeController> logger, DiscordRestClient discordRestClient, IConfiguration config) : base(discordRestClient, config)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new LandingModel();
            model.CurrentUser = await GetAuthenticatedUserInfoAsync();

            // should return the proper error codes if the user is not in the discord or muted

            return View(nameof(Index), model);
        }

        [Authorize]
        public async Task<IActionResult> Login()
        {
            return Redirect("/Courses");
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync();
            return Redirect(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
