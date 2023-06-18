using MongoDB.Driver;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models;

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
			if (await GetAssetById(asset.Id) != null || await GetAssetByName(asset.Name) != null)
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

		public async Task<OptifineAsset?> GetAssetByName(string name)
		{
			try
			{
				var filter = Builders<OptifineAsset>.Filter.Eq(x => x.Name, name);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<Dictionary<Guid, List<string>>> GetAllAssetIds()
		{
			var dictionary = new Dictionary<Guid, List<string>>();
			try
			{
				var filter = Builders<OptifineAsset>.Filter.Empty;
				var projection = Builders<OptifineAsset>.Projection.Include(p => p.Id).Include(p => p.Name);
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

		public async Task<string> GetAssetNameById(Guid id)
		{
			var filter = Builders<OptifineAsset>.Filter.Eq(p => p.Id, id);
			var projection = Builders<OptifineAsset>.Projection.Include(p => p.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}

		public async Task<bool> UpdateAsset(Guid id, string? name, string? path)
		{
			if (await GetAssetById(id) != null)
				return false;

			var filter = Builders<OptifineAsset>.Filter.Eq(p => p.Id, id);
			List<UpdateDefinition<OptifineAsset>> updateDefinitions = new();

			if (name != null)
				updateDefinitions.Add(Builders<OptifineAsset>.Update.Set(p => p.Name, name));

			if (path != null)
				updateDefinitions.Add(Builders<OptifineAsset>.Update.Set(p => p.Path, path));

			var update = Builders<OptifineAsset>.Update.Combine(updateDefinitions);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> AddAssetProperties(Guid id, OptifineProperties properties)
		{
			var filter = Builders<OptifineAsset>.Filter.Eq(a => a.Id, id);
			var update = Builders<OptifineAsset>.Update.Push(a => a.Properties, properties);

			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> UpdateAssetProperties(Guid assetId, Guid propertiesId, string? fileName, byte[]? data, MCVersion? mcVersion)
		{
			var filter = Builders<OptifineAsset>.Filter.Eq(a => a.Id, assetId) &
			             Builders<OptifineAsset>.Filter.ElemMatch(a => a.Properties, p => p.Id == propertiesId);

			var updateDefinition = Builders<OptifineAsset>.Update;
			List<UpdateDefinition<OptifineAsset>> updateDefinitions = new();

			if (fileName != null)
				updateDefinitions.Add(updateDefinition.Set(a => a.Properties[-1].FileName, fileName));

			if (data != null)
				updateDefinitions.Add(updateDefinition.Set(a => a.Properties[-1].Data, data));

			if (mcVersion != null)
				updateDefinitions.Add(updateDefinition.Set(a => a.Properties[-1].MCVersion, mcVersion));

			var update = Builders<OptifineAsset>.Update.Combine(updateDefinitions);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> RemoveAssetProperties(Guid assetId, Guid propertiesId)
		{
			var filter = Builders<OptifineAsset>.Filter.Eq(a => a.Id, assetId);
			var update = Builders<OptifineAsset>.Update.PullFilter(a => a.Properties, p => p.Id == propertiesId);

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
		Task<OptifineAsset?> GetAssetByName(string name);
		Task<Dictionary<Guid, List<string>>> GetAllAssetIds();
		Task<List<OptifineAsset>> GetAllAssets();
		Task<string> GetAssetNameById(Guid id);

		// Update
		Task<bool> UpdateAsset(Guid id, string? name, string? path);
		Task<bool> AddAssetProperties(Guid id, OptifineProperties properties);
		Task<bool> UpdateAssetProperties(Guid assetId, Guid propertiesId, string? fileName, byte[]? data, MCVersion? mcVersion);
		Task<bool> RemoveAssetProperties(Guid assetId, Guid propertiesId);

		// Delete
		Task<bool> DeleteAsset(Guid id);
	}
}
