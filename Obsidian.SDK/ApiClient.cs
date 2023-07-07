using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Newtonsoft.Json;
using Obsidian.SDK.Models.Auth;

namespace Obsidian.SDK
{
	public class ApiClient : IApiClient
	{
		private readonly HttpClient _httpClient;
		private string? _bearerToken;
		private readonly string _baseUrl;
		private readonly LoginModel? _botLogin;

		public ApiClient(HttpClient httpClient, string baseUrl)
		{
			_httpClient = httpClient;
			_baseUrl = baseUrl;
		}

		public ApiClient(HttpClient httpClient, string baseUrl, LoginModel botLogin) : this(httpClient, baseUrl)
		{
			_botLogin = botLogin;
			SetBearerToken().Wait();
		}

		public ApiClient(HttpClient httpClient, string baseUrl, string bearerToken) : this(httpClient, baseUrl)
		{
			_bearerToken = bearerToken;
		}

		private async Task<bool> SetBearerToken()
		{
			if (_botLogin == null)
				return false;

			string? bearerToken = await PostAsync<string>("api/v1/auth/login", _botLogin, true);
			if (string.IsNullOrEmpty(bearerToken))
				return false;

			_bearerToken = bearerToken;
			return true;
		}

		public async Task<T?> GetAsync<T>(string url, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100)
			=> await PerformHttpRequest<T?>(_httpClient.GetAsync(GetUrl(url)), allowAnonymous, retryCount, delayMilliseconds);

		public async Task<T?> PostAsync<T>(string url, object? content, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100)
			=> await PerformHttpRequest<T?>(_httpClient.PostAsync(GetUrl(url), JsonContent.Create(content)), allowAnonymous, retryCount, delayMilliseconds);

		public async Task<T?> PutAsync<T>(string url, object? content, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100)
			=> await PerformHttpRequest<T?>(_httpClient.PutAsync(GetUrl(url), JsonContent.Create(content)), allowAnonymous, retryCount, delayMilliseconds);

		public async Task<T?> DeleteAsync<T>(string url, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100)
			=> await PerformHttpRequest<T?>(_httpClient.DeleteAsync(GetUrl(url)), allowAnonymous, retryCount, delayMilliseconds);

		private async Task<T?> PerformHttpRequest<T>(Task<HttpResponseMessage> httpAction, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100)
		{
			int retry = 0;
			while (retry < retryCount)
			{
				try
				{
					_httpClient.DefaultRequestHeaders.Authorization = !allowAnonymous ? new AuthenticationHeaderValue("Bearer", _bearerToken) : null;
					HttpResponseMessage response = await httpAction;

					if (response.IsSuccessStatusCode)
					{
						var responseContent = await response.Content.ReadAsStringAsync();

						if (Type.GetTypeCode(typeof(T)) == TypeCode.String)
							return (T)(object)responseContent;

						return JsonConvert.DeserializeObject<T>(responseContent); // System.Net.Json doesn't work reliably, using Newtonsoft instead
					}
					if (response.StatusCode == HttpStatusCode.Unauthorized)
						await SetBearerToken();
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
			throw new HttpRequestException($"API request failed after {retryCount} retries.");
		}

		private string GetUrl(string url)
			=> $"{_baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
	}

	public interface IApiClient
	{
		Task<T?> GetAsync<T>(string url, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100);
		Task<T?> PostAsync<T>(string url, object? content, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100);
		Task<T?> PutAsync<T>(string url, object? content, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100);
		Task<T?> DeleteAsync<T>(string url, bool allowAnonymous = false, int retryCount = 3, int delayMilliseconds = 100);
	}
}
