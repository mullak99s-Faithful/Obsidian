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
			string fullUrl = $"{_endpoint}/api/{url.TrimStart('/')}";
			int retry = 0;

			while (retry < retryCount)
			{
				if (!allowAnonymous)
				{
					AccessTokenResult? accessTokenResult = await _accessTokenProvider.RequestAccessToken();
					if (!accessTokenResult.TryGetToken(out var accessToken))
						throw new HttpRequestException("You need to be logged in!");
					_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
				}

				try
				{
					var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
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

				// If the access token is not available or the request fails, retry after a delay
				await Task.Delay(delayMilliseconds);
				retry++;
			}

			// If the API request failed after retrying, return null or throw an exception
			throw new HttpRequestException($"API request to {fullUrl} failed after {retryCount} retries.");
		}

	}
}
