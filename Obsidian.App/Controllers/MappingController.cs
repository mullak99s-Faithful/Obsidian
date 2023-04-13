using Obsidian.App.Client;
using Obsidian.SDK.Models;

namespace Obsidian.App.Controllers
{
	public class MappingController
	{
		private ApiService _apiService { get; }

		public MappingController(ApiService apiService)
		{
			_apiService = apiService;
		}

		public async Task<List<Guid>> GetAllMappingIds()
			=> (await _apiService.GetAsync<IEnumerable<Guid>>("mapping/texturemap/getallids", true) ?? new List<Guid>()).ToList();

		public async Task<string> GetMappingName(Guid id)
			=> await _apiService.GetAsync<string>($"mapping/texturemap/getname/{id}", true) ?? string.Empty;

		public async Task<TextureMapping> GetTextureMapping(Guid id)
			=> await _apiService.GetAsync<TextureMapping>($"mapping/texturemap/get/{id}", true) ?? new TextureMapping();
	}
}
