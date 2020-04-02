using CSSCourseManagementWeb.Models;
using Discord.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Controllers
{
    public class CoursesController : DiscordControllerBase
    {
        private readonly ILogger<CoursesController> _logger;
        public CoursesController(ILogger<CoursesController> logger, DiscordRestClient discordRestClient, IConfiguration config) : base(discordRestClient, config)
        {
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetAuthenticatedUserInfoAsync();
            if (!currentUser.InGuild || currentUser.IsMuted)
            {
                return Unauthorized("You are either muted or not in this guild; so you cannot join any channels.");
            }
            var currentUserRoles = await GetRolesForUserAsync(currentUser.CurrentUser.Id);

            var model = new RoleModel();
            var storage = GetStorageUtil();
            model.CurrentUser = currentUser;

            // get all channels
            var courses = await storage.GetCoursesAsync(GuildId);
            foreach (var course in courses)
            {
                if (currentUserRoles.Contains(course.RoleId))
                {
                    // user is in this course
                    model.LeaveableCourses.Add(course);
                }
                else
                {
                    model.JoinableCourses.Add(course);
                }
            }

            return View(nameof(Index), model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Join(string id)
        {
            var currentUser = await GetAuthenticatedUserInfoAsync();
            if (!currentUser.InGuild || currentUser.IsMuted)
            {
                return Unauthorized("You are either muted or not in this guild; so you cannot join any channels.");
            }

            var storage = GetStorageUtil();
            var course = await storage.GetCourseAsync(GuildId, id);
            var roleId = ulong.Parse(course.RoleId);

            await AddRoleForUserAsync(currentUser.CurrentUser.Id, roleId);

            return Redirect("/Courses");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Leave(string id)
        {
            var currentUser = await GetAuthenticatedUserInfoAsync();
            if (!currentUser.InGuild || currentUser.IsMuted)
            {
                // TODO: make these error pages look nicer
                return Unauthorized("You are either muted or not in this guild; so you cannot join any channels.");
            }

            var storage = GetStorageUtil();
            var course = await storage.GetCourseAsync(GuildId, id);
            var roleId = ulong.Parse(course.RoleId);

            await RemoveRoleForUserAsync(currentUser.CurrentUser.Id, roleId);

            return Redirect("/Courses");
        }
    }
}
