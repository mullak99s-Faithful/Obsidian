using MongoDB.Driver;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Minecraft;

namespace Obsidian.API.Repository
{
    public class ModelMapRepository : IModelMapRepository
	{
		private readonly IMongoCollection<ModelMapping> _collection;

		public ModelMapRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<ModelMapping>("ModelMapping");
		}

		#region Create
		public async Task AddModelMap(ModelMapping modelMapping)
			=> await _collection.InsertOneAsync(modelMapping);
		#endregion

		#region Read
		public async Task<ModelMapping?> GetModelMappingById(Guid id)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<List<ModelMapping>> GetModelMappingsByName(string name)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Name, name);
				return await _collection.Find(filter).ToListAsync();
			}
			catch (Exception)
			{
				return new List<ModelMapping>(0);
			}
		}

		public async Task<Dictionary<Guid, string>> GetAllModelMappingIds()
		{
			var dictionary = new Dictionary<Guid, string>();
			try
			{
				var filter = Builders<ModelMapping>.Filter.Empty;
				var projection = Builders<ModelMapping>.Projection.Include(t => t.Id).Include(t => t.Name);
				var cursor = await _collection.Find(filter).Project(projection).ToCursorAsync();

				while (await cursor.MoveNextAsync())
				{
					foreach (var document in cursor.Current)
						dictionary.Add(document["_id"].AsGuid, document["Name"].AsString);
				}
				return dictionary;
			}
			catch (Exception)
			{
				return dictionary;
			}
		}

		public async Task<List<ModelMapping>> GetAllModelMappings()
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Empty;
				var documents = await _collection.Find(filter).ToListAsync();
				return documents;
			}
			catch (Exception)
			{
				return new List<ModelMapping>(0);
			}
		}

		public async Task<string> GetModelMappingNameById(Guid id)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
			var projection = Builders<ModelMapping>.Projection.Include(t => t.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}
		#endregion

		#region Update
		public async Task<bool> UpdateNameById(Guid id, string newName)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
				var update = Builders<ModelMapping>.Update.Set(t => t.Name, newName);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> AddModel(ModelAsset asset, Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			existingMap.Models.Add(asset);

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsAcknowledged;
		}

		public async Task<bool> EditModel(BlockModel asset, Guid modelId, Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			ModelAsset? existingModel = existingMap.Models.Find(x => x.Id == modelId);
			if (existingModel == null)
				return false;

			existingModel.Update(asset);

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsAcknowledged;
		}

		public async Task<bool> DeleteModel(Guid modelId, Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			var model = existingMap.Models.Find(x => x.Id == modelId);
			if (model == null)
				return false;

			existingMap.Models.Remove(model);

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsAcknowledged;
		}

		#endregion

		#region Delete
		public async Task<bool> DeleteById(Guid id)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
				var deleted = await _collection.DeleteOneAsync(filter);
				return deleted.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface IModelMapRepository
	{
		// Create
		Task AddModelMap(ModelMapping modelMapping);

		// Read
		Task<ModelMapping?> GetModelMappingById(Guid id);
		Task<List<ModelMapping>> GetModelMappingsByName(string name);
		Task<Dictionary<Guid, string>> GetAllModelMappingIds();
		Task<List<ModelMapping>> GetAllModelMappings();
		Task<string> GetModelMappingNameById(Guid id);

		// Update
		Task<bool> UpdateNameById(Guid id, string newName);
		Task<bool> AddModel(ModelAsset asset, Guid modelMapId);
		Task<bool> EditModel(BlockModel asset, Guid modelId, Guid modelMapId);
		Task<bool> DeleteModel(Guid modelId, Guid modelMapId);

		// Delete
		Task<bool> DeleteById(Guid id);
	}
}
