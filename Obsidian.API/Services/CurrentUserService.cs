using System.Security.Claims;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Newtonsoft.Json.Linq;
using Obsidian.API.Abstractions;

namespace Obsidian.API.Services
{
	public interface ICurrentUserService
	{
		public Task<string[]> GetCurrentDiscordRolesAsync(string userId);
		public Task<Dictionary<string, string[]>> GetAllDiscordRolesAsync();
	}

	public class CurrentUserService : ICurrentUserService, IRoleValidator, IPermissionValidator
	{
		private readonly ManagementApiClient _managementApiClient;
		private readonly string _auth0ManagementApiAudience;

		public CurrentUserService(ManagementApiClient managementApiClient, string auth0ManagementApiAudience)
		{
			_managementApiClient = managementApiClient;
			_auth0ManagementApiAudience = auth0ManagementApiAudience;
		}

		public async Task<string[]> GetCurrentDiscordRolesAsync(string userId)
		{
			var user = await _managementApiClient.Users.GetAsync(userId, "app_metadata");

			var appMetadata = user.AppMetadata.ToJObject();
			return appMetadata["discord_roles"]?.ToObject<string[]>();
		}

		public async Task<Dictionary<string, string[]>> GetAllDiscordRolesAsync()
		{
			var users = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest
			{
				IncludeFields = true,
				Fields = "app_metadata"
			});

			var result = new Dictionary<string, string[]>();
			foreach (var user in users)
			{
				var appMetadata = user.AppMetadata.ToJObject();
				result[user.UserId] = appMetadata["discord_roles"]?.ToObject<string[]>();
			}

			return result;
		}

		public static async Task<CurrentUserService> CreateAsync(IConfiguration configuration)
		{
			var auth0ManagementApiAudience = configuration["Auth0:Audience"];
			var auth0ManagementApiClientId = configuration["Auth0:ClientId"];
			var auth0ManagementApiClientSecret = configuration["Auth0:ClientSecret"];
			var auth0Domain = configuration["Auth0:Authority"];

			var managementApiToken = await GetManagementApiTokenAsync(auth0Domain, auth0ManagementApiClientId, auth0ManagementApiClientSecret);
			var managementApiBaseUri = new Uri($"{auth0Domain}/api/v2/");
			var managementApiClient = new ManagementApiClient(managementApiToken, managementApiBaseUri);

			return new CurrentUserService(managementApiClient, auth0ManagementApiAudience);
		}

		private static async Task<string> GetManagementApiTokenAsync(string auth0Domain, string managementApiClientId, string managementApiClientSecret)
		{
			using var httpClient = new HttpClient();
			var tokenRequest = new Dictionary<string, string>
			{
				["grant_type"] = "client_credentials",
				["client_id"] = managementApiClientId,
				["client_secret"] = managementApiClientSecret,
				["audience"] = $"{auth0Domain}/api/v2/"
			};
			var tokenResponse = await httpClient.PostAsync($"{auth0Domain}/oauth/token", new FormUrlEncodedContent(tokenRequest));

			if (!tokenResponse.IsSuccessStatusCode)
			{
				var errorResponseContent = await tokenResponse.Content.ReadAsStringAsync();
				var errorResponseJson = JObject.Parse(errorResponseContent);
				var errorMessage = errorResponseJson["error_description"].ToString();
				Console.WriteLine($"Error obtaining Management API access token: {errorMessage}");
				return null;
			}

			var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
			var tokenResponseJson = JObject.Parse(tokenResponseContent);
			return tokenResponseJson["access_token"].ToString();
		}

		public bool CurrentUserHasRole(ClaimsIdentity user, string name)
		{
			return user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == name);
		}

		public bool CurrentUserHasPermission(ClaimsIdentity user, string permission)
		{
			return user.HasClaim(c => c.Type == "permissions" && c.Value == permission);
		}

		public List<string> GetPermissions()
		{
			return new List<string>()
			{
				"admin-action",
				"read:user_idp_tokens",
				"read:users",
				"write:add-branch",
				"write:add-mapping",
				"write:add-pack",
				"write:delete-mapping",
				"write:delete-pack",
				"write:edit-pack",
				"write:generate-packs",
				"write:import-pack",
				"write:upload-texture",
				"write:delete-texture",
				"write:upload-model",
				"write:delete-model",
				"write:upload-blockstate",
				"write:delete-blockstate",
				"write:pregenerate-assets",
				"write:purge-assets"
			};
		}
	}
}
