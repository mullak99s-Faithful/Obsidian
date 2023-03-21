using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ObsidianAPI.Abstractions;
using ObsidianAPI.Extensions;
using ObsidianAPI.Logic;
using ObsidianAPI.Services;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ObsidianAPI
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			AuthConfig = Configuration.GetSection("Auth0").Get<Auth0Config>();
		}

		public Auth0Config AuthConfig { get; set; }
		public IConfiguration Configuration { get; }
		private CurrentUserService? _currentUserService;

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options => options.AddDefaultPolicy(new CorsPolicy { Origins = { Configuration["AllowedHosts"] } }));
			services.AddRouting(options => options.LowercaseUrls = true);
			services.AddControllers();

			services.AddAuthenticationWithAuth0(AuthConfig);

			//services.AddScoped<CurrentUserService>();
			//services.AddScoped<IRoleValidator, CurrentUserService>();
			var managementApiAudience = AuthConfig.Audience;
			services.AddSingleton(managementApiAudience);

			services.AddSingleton<IPackLogic, PackLogic>();
			services.AddSingleton<IMappingLogic, MappingLogic>();
			services.AddSingleton<ITextureLogic, TextureLogic>();

			BuildServiceProviderAsync(services).Wait();

			//services.AddRoleBasedAuthorizationWithRoles(_currentUserService.GetRolePolicies());
			services.AddPermissionBasedAuthorizationWithPermissions(_currentUserService.GetPermissions());

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Obsidian", Version = "v1" });
				c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.OAuth2,
					Flows = new OpenApiOAuthFlows
					{
						Implicit = new OpenApiOAuthFlow
						{
							AuthorizationUrl = new Uri($"{AuthConfig.Authority}authorize?audience={AuthConfig.Audience}"),
							Scopes = new Dictionary<string, string>
							{
								{ "openid", "OpenID" }
							}
						}
					}
				});
				c.OperationFilter<SecurityRequirementsOperationFilter>();
			});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c =>
				{
					c.DefaultModelRendering(ModelRendering.Model);
					c.SwaggerEndpoint("/swagger/v1/swagger.json", "ObsidianAPI");
					c.OAuthClientId(AuthConfig.ClientId);
				});
			}

			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseCors();
			app.UseAuthentication();

			//app.UseRoles(_currentUserService!.GetRoles());
			app.UsePermissions(_currentUserService!.GetPermissions());

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		private async Task BuildServiceProviderAsync(IServiceCollection services)
		{
			var provider = services.BuildServiceProvider();

			var configuration = provider.GetService<IConfiguration>();
			_currentUserService = await CurrentUserService.CreateAsync(configuration);

			services.AddSingleton<IRoleValidator>(_currentUserService);
			services.AddSingleton<IPermissionValidator>(_currentUserService);
			services.AddSingleton<ICurrentUserService>(_currentUserService);
		}
	}
}
