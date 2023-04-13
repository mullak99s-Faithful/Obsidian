using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Obsidian.App.Client
{
	public class ApiService
	{
		private readonly HttpClient _httpClient;
		private readonly IAccessTokenProvider _accessTokenProvider;

		private readonly string _endpoint;

		public ApiService(HttpClient httpClient, IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_accessTokenProvider = accessTokenProvider;
			_endpoint = configuration["Endpoint"].TrimEnd('/');
		}

		public async Task<T?> GetAsync<T>(string url, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100)
		{
			var accessTokenResult = await _accessTokenProvider.RequestAccessToken();
			var retry = 0;

			while (retry < retryCount)
			{
				AccessToken? accessToken = null;
				if (allowAnonymous || accessTokenResult.TryGetToken(out accessToken))
				{
					if (!allowAnonymous && accessToken != null)
						_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);

					try
					{
						var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}/api/{url.TrimStart('/')}");
						request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
						var response = await _httpClient.SendAsync(request);

						if (response.IsSuccessStatusCode)
						{
							var responseContent = await response.Content.ReadAsStringAsync();
							return JsonConvert.DeserializeObject<T>(responseContent); // System.Net.Json doesn't work reliably, using Newtonsoft instead
						}
					}
					catch (HttpRequestException ex)
					{
						throw new HttpRequestException("Could not communicate with the API. Is the API online?", ex);
					}
				}

				// If the access token is not available or the request fails, retry after a delay
				await Task.Delay(delayMilliseconds);
				retry++;
				accessTokenResult = await _accessTokenProvider.RequestAccessToken();
			}

			// If the API request failed after retrying, return null or throw an exception
			throw new HttpRequestException($"API request to {url} failed after {retryCount} retries.");
		}
	}
}
