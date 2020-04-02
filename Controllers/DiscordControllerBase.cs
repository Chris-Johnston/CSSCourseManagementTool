using CSSCourseManagementWeb.Models;
using Discord;
using Discord.Rest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Controllers
{
    public abstract class DiscordControllerBase : Controller
    {
        // this is shared for all application stuff, per-user bearer client is different
        private readonly DiscordRestClient discordRestAppClient;
        private readonly IConfiguration configuration;

        // currently will only target a single guild, may want to refactor if multiple guilds are planned
        public readonly ulong GuildId;
        // assumption that all course channels are under the same category id
        public readonly ulong CategoryId;

        public DiscordControllerBase(DiscordRestClient discordRestClient, IConfiguration configuration) : base() // DI
        {
            this.discordRestAppClient = discordRestClient;
            this.configuration = configuration;

            GuildId = ulong.Parse(configuration[ConfigConstants.DiscordGuildId]);
            CategoryId = ulong.Parse(configuration[ConfigConstants.DiscordCategoryId]);
        }

        public StorageUtil GetStorageUtil()
        {
            return new StorageUtil(configuration[ConfigConstants.AzureStorageConnectionString]);
        }

        // this has to be unique per user
        public async Task<DiscordUserInfo> GetAuthenticatedUserInfoAsync()
        {

            var token = await HttpContext.GetTokenAsync("Discord", "access_token");
            if (token == null) return null;

            using var client = new DiscordRestClient();
            await client.LoginAsync(TokenType.Bearer, token, true);
            var guildSummaries = await client.GetGuildSummariesAsync().FlattenAsync();
            var targetGuild = guildSummaries.FirstOrDefault(x => x.Id == GuildId);

            var result = new DiscordUserInfo()
            {
                Guild = targetGuild,
                CurrentUser = client.CurrentUser,
            };

            return result;
        }

        public async Task<DiscordRestClient> GetAppClientAsync()
        {
            if (this.discordRestAppClient.LoginState == LoginState.LoggedOut)
            {
                await discordRestAppClient.LoginAsync(TokenType.Bot, configuration[ConfigConstants.DiscordAppBotToken]);
            }

            return discordRestAppClient;
        }

        public async Task<RestGuild> GetRestGuildAsync()
        {
            var client = await GetAppClientAsync();
            return await client.GetGuildAsync(GuildId);
        }

        public async Task<List<string>> GetRolesForUserAsync(ulong userId) // treat role Ids as strings because storage doesn't like ulong
        {
            var guild = await GetRestGuildAsync();

            var user = await guild.GetUserAsync(userId) as RestGuildUser;
            var rolenames = new List<string>();

            foreach (var roleId in user.RoleIds)
            {
                var role = guild.GetRole(roleId);
                rolenames.Add(role.Id.ToString());
            }
            return rolenames;
        }

        public async Task AddRoleForUserAsync(ulong userId, ulong roleId)
        {
            var guild = await GetRestGuildAsync();

            var user = await guild.GetUserAsync(userId) as RestGuildUser;
            var role = guild.GetRole(roleId);
            var requestOptions = new RequestOptions()
            {
                AuditLogReason = "User self-added course (role/channel) online.",
            };
            await user.AddRoleAsync(role, requestOptions);
        }

        public async Task RemoveRoleForUserAsync(ulong userId, ulong roleId)
        {
            var guild = await GetRestGuildAsync();

            var user = await guild.GetUserAsync(userId) as RestGuildUser;
            var role = guild.GetRole(roleId);
            var requestOptions = new RequestOptions()
            {
                AuditLogReason = "User self-removed course (role/channel) online.",
            };
            await user.RemoveRoleAsync(role, requestOptions);
        }

    }
}
