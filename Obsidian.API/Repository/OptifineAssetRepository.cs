using MongoDB.Driver;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;

namespace Obsidian.API.Repository
{
	public class OptifineAssetRepository : IOptifineAssetRepository
	{
		private readonly IMongoCollection<OptifineAsset> _collection;

		public OptifineAssetRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<OptifineAsset>("OptifineAsset");
		}

		public async Task<bool> AddAsset(OptifineAsset asset)
		{
			if (await GetAssetById(asset.Id) != null || await GetAssetByNames(asset.Names) != null)
				return false;

			await _collection.InsertOneAsync(asset);
			return true;
		}

		public async Task<OptifineAsset?> GetAssetById(Guid id)
		{
			try
			{
				var filter = Builders<OptifineAsset>.Filter.Eq(t => t.Id, id);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<OptifineAsset?> GetAssetByNames(List<string> names)
		{
			try
			{
				var filter = Builders<OptifineAsset>.Filter.AnyIn(x => x.Names, names);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<OptifineAsset?> GetAssetByName(string name)
			=> await GetAssetByNames(new List<string> { name });

		public async Task<Dictionary<Guid, List<string>>> GetAllAssetIds()
		{
			var dictionary = new Dictionary<Guid, List<string>>();
			try
			{
				var filter = Builders<OptifineAsset>.Filter.Empty;
				var projection = Builders<OptifineAsset>.Projection.Include(p => p.Id).Include(p => p.Names);
				var cursor = await _collection.Find(filter).Project(projection).ToCursorAsync();

				while (await cursor.MoveNextAsync())
				{
					foreach (var document in cursor.Current)
					{
						var id = document["_id"].AsGuid;
						var names = document["Names"].AsBsonArray.Select(x => x.AsString).ToList();
						dictionary.Add(id, names);
					}
				}
				return dictionary;
			}
			catch (Exception)
			{
				return dictionary;
			}
		}

		public async Task<List<OptifineAsset>> GetAllAssets()
		{
			try
			{
				var filter = Builders<OptifineAsset>.Filter.Empty;
				var documents = await _collection.Find(filter).ToListAsync();
				return documents;
			}
			catch (Exception)
			{
				return new List<OptifineAsset>(0);
			}
		}

		public async Task<List<string>> GetAssetNamesById(Guid id)
		{
			var filter = Builders<OptifineAsset>.Filter.Eq(p => p.Id, id);
			var projection = Builders<OptifineAsset>.Projection.Include(p => p.Names);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			var names = document["Names"].AsBsonArray.Select(x => x.AsString).ToList();
			return names;
		}

		public async Task<bool> UpdateAsset(Guid id, List<string>? names, MCVersion? version, string? path)
		{
			if (await GetAssetById(id) != null)
				return false;

			var filter = Builders<OptifineAsset>.Filter.Eq(p => p.Id, id);
			List<UpdateDefinition<OptifineAsset>> updateDefinitions = new();

			if (names != null)
				updateDefinitions.Add(Builders<OptifineAsset>.Update.Set(p => p.Names, names));

			if (version != null)
				updateDefinitions.Add(Builders<OptifineAsset>.Update.Set(p => p.MCVersion, version));

			if (path != null)
				updateDefinitions.Add(Builders<OptifineAsset>.Update.Set(p => p.Path, path));

			var update = Builders<OptifineAsset>.Update.Combine(updateDefinitions);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> DeleteAsset(Guid id)
		{
			if (await GetAssetById(id) != null)
				return false;

			var filter = Builders<OptifineAsset>.Filter.Eq(p => p.Id, id);
			return (await _collection.DeleteOneAsync(filter)).DeletedCount > 0;
		}
	}

	public interface IOptifineAssetRepository
	{
		// Create
		Task<bool> AddAsset(OptifineAsset asset);

		// Read
		Task<OptifineAsset?> GetAssetById(Guid id);
		Task<OptifineAsset?> GetAssetByNames(List<string> names);
		Task<OptifineAsset?> GetAssetByName(string name);
		Task<Dictionary<Guid, List<string>>> GetAllAssetIds();
		Task<List<OptifineAsset>> GetAllAssets();
		Task<List<string>> GetAssetNamesById(Guid id);

		// Update
		Task<bool> UpdateAsset(Guid id, List<string>? names, MCVersion? version, string? path);

		// Delete
		Task<bool> DeleteAsset(Guid id);
	}
}
