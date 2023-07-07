using Obsidian.SDK.Models;

namespace Obsidian.SDK.Controllers
{
	public class PackController
	{
		private readonly ApiClient _apiClient;

		public PackController(ApiClient apiClient)
		{
			_apiClient = apiClient;
		}

		public async Task<IEnumerable<Pack>> GetAll()
			=> await _apiClient.GetAsync<IEnumerable<Pack>>("api/v1/pack/getall") ?? new List<Pack>();
	}
}
