using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Mvc;
using Obsidian.SDK.Models.Auth;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class AuthController : ControllerBase
	{
		private IAuthenticationApiClient _authApiClient;
		private Auth0Config _auth0Config;

		public AuthController(IAuthenticationApiClient authApiClient, Auth0Config auth0Config)
		{
			_authApiClient = authApiClient;
			_auth0Config = auth0Config;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
		{
			try
			{
				var loginResult = await _authApiClient.GetTokenAsync(new ResourceOwnerTokenRequest
				{
					ClientId = _auth0Config.ClientId,
					ClientSecret = _auth0Config.ClientSecret,
					Realm = "Username-Password-Authentication", // Specify the correct name of your DB connection
					Scope = "openid profile", // Ensure that this includes the scopes required for your application
					Username = loginModel.Username,
					Password = loginModel.Password
				});

				if (!string.IsNullOrEmpty(loginResult.AccessToken))
					return Ok(loginResult.AccessToken);
				return Unauthorized();
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}
