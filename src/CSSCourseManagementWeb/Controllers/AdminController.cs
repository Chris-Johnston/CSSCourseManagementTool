﻿using CSSCourseManagementWeb.Models;
using Discord;
using Discord.Net;
using Discord.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Controllers
{
    public class AdminController : DiscordControllerBase
    {
        private readonly ILogger<AdminController> logger;
        public AdminController(ILogger<AdminController> logger, DiscordRestClient discordRestClient, IConfiguration config) : base(discordRestClient, config)
        {
            this.logger = logger;
        }

        // lists all existing course channels
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            DiscordUserInfo currentUser = null;

            try
            {
                currentUser = await GetAuthenticatedUserInfoAsync();
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == HttpStatusCode.Unauthorized)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                return Unauthorized("Something broke, your login may be unauthorized.");
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == HttpStatusCode.NotFound)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                return NotFound("Something wasn't found.");
            }
            catch (HttpException httpEx)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                throw;
            }

            if (currentUser == null)
            {
                logger.LogError("Current User is null, this indicates some issue with logging in.");
                return Unauthorized("Unable to log in.");
            }

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
            DiscordUserInfo currentUser = null;

            try
            {
                currentUser = await GetAuthenticatedUserInfoAsync();
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == HttpStatusCode.Unauthorized)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                return Unauthorized("Something broke, your login may be unauthorized.");
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == HttpStatusCode.NotFound)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                return NotFound("Something wasn't found.");
            }
            catch (HttpException httpEx)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                throw;
            }

            if (currentUser == null)
            {
                logger.LogError("Current User is null, this indicates some issue with logging in.");
                return Unauthorized("Unable to log in.");
            }
            if (!currentUser.InGuild)
                return Unauthorized("You are not a member of this server.");
            if (!currentUser.IsAdmin)
                return Unauthorized("You must be an admin to view this page.");

            var exists = await CheckExistsAndValidAsync(roleId, channelId);
            if (!exists)
            {
                return BadRequest("Either the role or channel Id did not exist in this guild, or the role had permissions that could not be granted.");
            }

            courseName = NormalizeCourseChannelName(courseName);

            var courseEntity = new CourseEntity()
            {
                GuildId = GuildId.ToString(),
                CourseId = courseName,
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
            DiscordUserInfo currentUser = null;

            try
            {
                currentUser = await GetAuthenticatedUserInfoAsync();
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == HttpStatusCode.Unauthorized)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                return Unauthorized("Something broke, your login may be unauthorized.");
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == HttpStatusCode.NotFound)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                return NotFound("Something wasn't found.");
            }
            catch (HttpException httpEx)
            {
                logger.LogError(httpEx, "Unhandled Discord HTTP Exception");
                throw;
            }

            if (currentUser == null)
            {
                logger.LogError("Current User is null, this indicates some issue with logging in.");
                return Unauthorized("Unable to log in.");
            }
            if (!currentUser.InGuild)
                return Unauthorized("You are not a member of this server.");
            if (!currentUser.IsAdmin)
                return Unauthorized("You must be an admin to view this page.");

            var appClient = await GetAppClientAsync();
            var results = await CreateCourseChannelAsync(appClient, courseName, 
                $"Created by {currentUser.CurrentUser.Username}#{currentUser.CurrentUser.Discriminator} {currentUser.CurrentUser.Id} online for course {courseName}.");

            return await CreateExisting(results.role.Id, results.channel.Id, courseName);
        }

        public async Task<IActionResult> Nuke(ulong channelId, string channelName)
        {
            // TODO: nuke and rebuild feature
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
            courseName = NormalizeCourseChannelName(courseName);

            var roleName = $"member_{channelCategory.Name}_{courseName}".ToLower();
            var role = await CreateRoleAsync(guild, roleName, requestOptions);
            if (role == null)
            {
                // duplicate
                throw new Exception("Cannot create a duplicate role.");
            }

            var channel = await CreateTextChannelAsync(guild, courseName, requestOptions, role.Id);
            if (channel == null)
            {
                throw new Exception("Cannot create a duplicate channel.");
            }

            await channel.SendMessageAsync(auditLogMessage);

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

            var role = await guild.CreateRoleAsync(roleName, permissions: GuildPermissions.None, options: options, isHoisted: false, isMentionable: true);
            return role;
        }

        private async Task<RestTextChannel> CreateTextChannelAsync(RestGuild guild, string courseName, RequestOptions requestOptions, ulong roleId)
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

            var category = await guild.GetChannelAsync(CategoryId);

            var overwriteList = new List<Overwrite>();
            overwriteList.AddRange(category.PermissionOverwrites);
            overwriteList.Add(new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, new Discord.OverwritePermissions(viewChannel: Discord.PermValue.Deny)));
            overwriteList.Add(new Overwrite(roleId, PermissionTarget.Role, new Discord.OverwritePermissions(viewChannel: Discord.PermValue.Allow)));

            var result = await guild.CreateTextChannelAsync(courseName, options =>
            {
                options.CategoryId = CategoryId;
                options.Topic = $"Course channel for {courseName}";
                options.PermissionOverwrites = overwriteList;

            }, options: requestOptions);
            return result;
        }

        private async Task<RestCategoryChannel> GetChannelCategory(DiscordRestClient client)
        {
            var guild = await client.GetGuildAsync(GuildId);
            return await guild.GetChannelAsync(CategoryId) as RestCategoryChannel;
        }

        // TODO: include a way to delete course channels
    }
}
