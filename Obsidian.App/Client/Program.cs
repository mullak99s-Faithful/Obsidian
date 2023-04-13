using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Obsidian.App.Controllers;

namespace Obsidian.App.Client
{
    public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");
			builder.RootComponents.Add<HeadOutlet>("head::after");

			// API
			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			builder.Services.AddScoped<ApiService>();

			// Controllers
			builder.Services.AddScoped<MappingController>();

			// Auth0 Authentication
			builder.Services.AddOidcAuthentication(options =>
			{
				builder.Configuration.Bind("Auth0", options.ProviderOptions);
				options.ProviderOptions.ResponseType = "code";
			});

			await builder.Build().RunAsync();
		}
	}
}