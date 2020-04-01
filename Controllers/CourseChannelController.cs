using CSSCourseManagementWeb.Models;
using Discord;
using Discord.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Controllers
{
    // TODO rename this to be the admin page
    public class CourseChannelController : DiscordControllerBase
    {
        private readonly ILogger<CourseChannelController> _logger;
        public CourseChannelController(ILogger<CourseChannelController> logger, DiscordRestClient discordRestClient, IConfiguration config) : base(discordRestClient, config)
        {
            _logger = logger;
        }

        // lists all existing course channels
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetAuthenticatedUserInfoAsync();
            if (!currentUser.InGuild)
                return Unauthorized("You are not a member of this server.");
            if (!currentUser.IsAdmin)
                return Unauthorized("You must be an admin to view this page.");

            var model = new CourseChannelList();
            var storage = GetStorageUtil();

            var courses = await storage.GetCoursesAsync(GuildId);
            model.Courses = courses;

            return View(nameof(Index), model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExisting(ulong roleId, ulong channelId, string courseName)
        {
            var currentUser = await GetAuthenticatedUserInfoAsync();
            if (!currentUser.InGuild)
                return Unauthorized("You are not a member of this server.");
            if (!currentUser.IsAdmin)
                return Unauthorized("You must be an admin to view this page.");

            var exists = await CheckExistsAndValidAsync(roleId, channelId);
            if (!exists)
            {
                return BadRequest("Either the role or channel Id did not exist in this guild, or the role had permissions that could not be granted.");
            }

            var courseEntity = new CourseEntity()
            {
                GuildId = GuildId.ToString(),
                CourseId = courseName.Trim().ToLower(),
                ChannelId = channelId.ToString(),
                RoleId = roleId.ToString(),
            };

            var storage = GetStorageUtil();
            var table = await storage.GetCourseTableAsync();

            var operation = TableOperation.InsertOrMerge(courseEntity);
            await table.ExecuteAsync(operation);

            // show the list
            return await Index();
        }

        private async Task<bool> CheckExistsAndValidAsync(ulong roleId, ulong channelId)
        {
            try
            {
                var guild = await GetRestGuildAsync();
                var role = guild.GetRole(roleId);

                // for any of these conditions, this role cannot be used
                if (role.Permissions.Administrator || role.Permissions.ManageChannels || role.Permissions.ManageGuild || role.Permissions.ManageRoles || role.IsEveryone)
                    return false;

                await guild.GetChannelAsync(channelId);

                // no thrown exception, OK
                return role != null;
            }
            catch (Exception)
            {
            }

            // caught exception
            return false;
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNew(string courseName)
        {
            var currentUser = await GetAuthenticatedUserInfoAsync();
            if (!currentUser.InGuild)
                return Unauthorized("You are not a member of this server.");
            if (!currentUser.IsAdmin)
                return Unauthorized("You must be an admin to view this page.");

            var appClient = await GetAppClientAsync();
            var results = await CreateCourseChannelAsync(appClient, courseName, $"Created by {currentUser.CurrentUser.Username}#{currentUser.CurrentUser.Discriminator} {currentUser.CurrentUser.Id} online for course {courseName}.");

            return await CreateExisting(results.role.Id, results.channel.Id, courseName);
        }

        public async Task<IActionResult> Nuke(ulong channelId, string channelName)
        {
            // TODO: nuke and rebuild
            return null;
        }

        private async Task<(RestRole role, RestChannel channel)> CreateCourseChannelAsync(DiscordRestClient client, string courseName, string auditLogMessage)
        {
            var requestOptions = new RequestOptions()
            {
                AuditLogReason = auditLogMessage,
            };
            var guild = await client.GetGuildAsync(GuildId);
            var channelCategory = await GetChannelCategory(client);

            var roleName = $"member_{channelCategory.Name}_{courseName}".ToLower();
            var role = await CreateRoleAsync(guild, roleName, requestOptions);
            if (role == null)
            {
                // duplicate
                throw new Exception("Cannot create a duplicate role.");
            }

            var channel = await CreateTextChannelAsync(guild, courseName, requestOptions);
            if (channel == null)
            {
                throw new Exception("Cannot create a duplicate channel.");
            }

            // deny all
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                new Discord.OverwritePermissions(viewChannel: Discord.PermValue.Deny), options: requestOptions);

            // allow the role
            await channel.AddPermissionOverwriteAsync(role,
                new Discord.OverwritePermissions(viewChannel: Discord.PermValue.Allow), options: requestOptions);

            return (role, channel);
        }

        private async Task<RestRole> CreateRoleAsync(RestGuild guild, string roleName, RequestOptions options)
        {
            // prevent duplicates
            if (guild.Roles.Any(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
            {
                // duplicate role name
                return null;
            }

            var role = await guild.CreateRoleAsync(roleName, permissions: GuildPermissions.None, options: options);
            await role.ModifyAsync(x => x.Mentionable = true, options: options);
            return role;
        }

        private async Task<RestTextChannel> CreateTextChannelAsync(RestGuild guild, string courseName, RequestOptions requestOptions)
        {
            // prevent duplicate under this category
            foreach (var channel in await guild.GetChannelsAsync())
            {
                if (channel.Name.Equals(courseName, StringComparison.OrdinalIgnoreCase) && channel is RestCategoryChannel restCategory && restCategory.Id == CategoryId)
                {
                    // there's a match under this category
                    return null;
                }
            }

            return await guild.CreateTextChannelAsync(courseName, options =>
            {
                options.CategoryId = CategoryId;
                options.Topic = $"Course channel for {courseName}";
            }, options: requestOptions);
        }

        private async Task<RestCategoryChannel> GetChannelCategory(DiscordRestClient client)
        {
            var guild = await client.GetGuildAsync(GuildId);
            return await guild.GetChannelAsync(CategoryId) as RestCategoryChannel;
        }
        // admin only
        // list all existing course channels
        // create new course channel
        // import existing course channel and role
        // wipe all course channel


        // partition key should be guild
        // row key can be user id
    }
}
