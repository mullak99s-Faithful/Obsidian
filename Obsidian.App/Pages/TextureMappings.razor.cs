using Microsoft.AspNetCore.Components;
using Obsidian.App.Controllers;
using Obsidian.SDK.Models;

namespace Obsidian.App.Pages
{
	public partial class TextureMappings
	{
		[Inject] private MappingController MappingController { get; set; }

		private readonly Dictionary<Guid, string?> _mappings = new();
		private Guid? _selectedMapping;
		private bool _loadingMapping;

		private TextureMapping? _textureMapping;

		protected override async Task OnInitializedAsync()
		{
			List<Guid> mapIds = await MappingController.GetAllMappingIds();
			List<Task> tasks = new();

			foreach (Guid mapId in mapIds)
			{
				tasks.Add(Task.Run(async () =>
				{
					string mapName = await MappingController.GetMappingName(mapId);
					_mappings.Add(mapId, mapName);
				}));
			}
			await Task.WhenAll(tasks);
		}

		private async Task SelectMapping(string id)
		{
			if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out Guid guid))
			{
				KeyValuePair<Guid, string?> map = _mappings.FirstOrDefault(x => x.Key == guid);
				await LoadMapping(guid);
			}
		}

		private async Task LoadMapping(Guid id)
		{
			_selectedMapping = id;
			_loadingMapping = true;
			_textureMapping = await MappingController.GetTextureMapping(id);
			_loadingMapping = false;
		}
	}
}
