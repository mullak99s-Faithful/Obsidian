using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Obsidian.API.Abstractions;
using Obsidian.API.Cache;
using Obsidian.API.Extensions;
using Obsidian.API.Git;
using Obsidian.API.Logic;
using Obsidian.API.Repository;
using Obsidian.API.Services;
using Octokit;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Obsidian.API
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
			services.AddCors(options =>
			{
				options.AddDefaultPolicy(builder =>
				{
					builder.WithOrigins(Configuration["AllowedHosts"])
						.AllowAnyMethod()
						.AllowAnyHeader()
						.WithHeaders("Authorization");
				});
			});
			services.AddRouting(options => options.LowercaseUrls = true);
			services.AddControllers();

			services.AddAuthenticationWithAuth0(AuthConfig);

			var managementApiAudience = AuthConfig.Audience;
			services.AddSingleton(managementApiAudience);

			services.AddSingleton<IMongoClient>(_ =>
			{
				MongoClientSettings? clientSettings = MongoClientSettings.FromConnectionString(Configuration.GetConnectionString("MongoDb"));
				clientSettings.MaxConnectionPoolSize = 500;
				return new MongoClient(clientSettings);
			});

			services.AddSingleton(s => {
				var client = s.GetRequiredService<IMongoClient>();
				var database = client.GetDatabase("Obsidian");
				return database;
			});

			services.AddHttpClient();
			services.Configure<HttpClientFactoryOptions>(options =>
			{
				options.HttpClientActions.Add(client =>
				{
					client.DefaultRequestHeaders.Add("User-Agent", Configuration["Common:UserAgent"]);
				});
			});

			GitOptions gitOptions = new GitOptions
			{
				PersonalAccessToken = Configuration["Tokens:GitHub"],
				AuthorName = Configuration["Common:Name"],
				AuthorEmail = Configuration["Common:Email"]
			};

			GitHubClient githubClient = new GitHubClient(new ProductHeaderValue(Configuration["Common:UserAgent"]))
			{
				Credentials = new Credentials(gitOptions.PersonalAccessToken)
			};

			services.AddSingleton<IGitOptions>(gitOptions);
			services.AddSingleton<IGitHubClient>(githubClient);

			// Cache
			services.AddSingleton<ITextureMapCache, TextureMapCache>();
			services.AddSingleton<IModelMapCache, ModelMapCache>();
			services.AddSingleton<IVersionAssetsCache, VersionAssetsCache>();

			// Repos
			services.AddScoped<ITextureMapRepository, TextureMapRepository>();
			services.AddScoped<IModelMapRepository, ModelMapRepository>();
			services.AddScoped<IBlockStateMapRepository, BlockStateMapRepository>();
			services.AddScoped<IPackRepository, PackRepository>();
			services.AddScoped<IVersionAssetsRepository, VersionAssetsRepository>();

			// Buckets
			services.AddScoped<ITextureBucket, TextureBucket>();
			services.AddScoped<IPackPngBucket, PackPngBucket>();
			services.AddScoped<IMiscBucket, MiscBucket>();

			// Logic
			services.AddScoped<ITextureLogic, TextureLogic>();
			services.AddScoped<IModelLogic, ModelLogic>();
			services.AddScoped<IBlockStateLogic, BlockStateLogic>();
			services.AddScoped<IPackLogic, PackLogic>();
			services.AddScoped<IPackPngLogic, PackPngLogic>();
			services.AddScoped<IMiscAssetLogic, MiscAssetLogic>();
			services.AddScoped<IToolsLogic, ToolsLogic>();
			services.AddScoped<IContinuousPackLogic, ContinuousPackLogic>();
			services.AddScoped<IAutoGenerationLogic, AutoGenerationLogic>();

			// Logic (Singleton)
			services.AddSingleton<IPackValidationLogic, PackValidationLogic>();

			BuildServiceProviderAsync(services).Wait();

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
							AuthorizationUrl = new Uri($"{AuthConfig.Authority}/authorize?audience={AuthConfig.Audience}"),
							Scopes = new Dictionary<string, string>
							{
								{ "openid", "OpenID" },
								{ "profile", "Profile" }
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
				app.UseDeveloperExceptionPage();

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.DefaultModelRendering(ModelRendering.Model);
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Obsidian.API");
				c.OAuthClientId(AuthConfig.ClientId);
			});

			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseCors();
			app.UseAuthentication();

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

			if (configuration != null)
			{
				_currentUserService = await CurrentUserService.CreateAsync(configuration);
				services.AddSingleton<IRoleValidator>(_currentUserService);
				services.AddSingleton<IPermissionValidator>(_currentUserService);
				services.AddSingleton<ICurrentUserService>(_currentUserService);

				services.AddPermissionBasedAuthorizationWithPermissions(_currentUserService.GetPermissions());
			}
		}
	}
}
