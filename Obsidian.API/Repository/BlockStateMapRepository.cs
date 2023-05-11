using MongoDB.Driver;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Mappings;

namespace Obsidian.API.Repository
{
	public class BlockStateMapRepository : IBlockStateMapRepository
	{
		private readonly IMongoCollection<BlockStateMapping> _collection;

		public BlockStateMapRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<BlockStateMapping>("BlockstateMapping");
		}

		#region Create
		public async Task AddBlockStateMap(BlockStateMapping modelMapping)
			=> await _collection.InsertOneAsync(modelMapping);
		#endregion

		#region Read
		public async Task<BlockStateMapping?> GetBlockStateMappingById(Guid id)
		{
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, id);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<List<BlockStateMapping>> GetBlockStateMappingsByName(string name)
		{
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Name, name);
				return await _collection.Find(filter).ToListAsync();
			}
			catch (Exception)
			{
				return new List<BlockStateMapping>(0);
			}
		}

		public async Task<Dictionary<Guid, string>> GetAllBlockStateMappingIds()
		{
			var dictionary = new Dictionary<Guid, string>();
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Empty;
				var projection = Builders<BlockStateMapping>.Projection.Include(t => t.Id).Include(t => t.Name);
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

		public async Task<List<BlockStateMapping>> GetAllBlockStateMappings()
		{
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Empty;
				var documents = await _collection.Find(filter).ToListAsync();
				return documents;
			}
			catch (Exception)
			{
				return new List<BlockStateMapping>(0);
			}
		}

		public async Task<string> GetBlockStateMappingNameById(Guid id)
		{
			var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, id);
			var projection = Builders<BlockStateMapping>.Projection.Include(t => t.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}

		public async Task<bool> DoesBlockStateExist(BlockState blockState, Guid blockStateMapId)
		{
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, blockStateMapId);

				var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
				BlockState? existingModel = existingMap.Models.Find(x => x.Id == blockState.Id || x.Name == blockState.Name || x.FileName == blockState.FileName);
				return existingModel != null;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion

		#region Update
		public async Task<bool> UpdateNameById(Guid id, string newName)
		{
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, id);
				var update = Builders<BlockStateMapping>.Update.Set(t => t.Name, newName);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> AddBlockState(BlockState asset, Guid blockStateMapId)
		{
			var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, blockStateMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			existingMap.Models.Add(asset);

			var update = Builders<BlockStateMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsAcknowledged;
		}

		public async Task<bool> EditBlockState(BlockState asset, Guid blockStateId, Guid blockStateMapId)
		{
			var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, blockStateMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			BlockState? existingModel = existingMap.Models.Find(x => x.Id == blockStateId);
			if (existingModel == null)
				return false;

			existingModel.Update(asset);

			var update = Builders<BlockStateMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsAcknowledged;
		}

		public async Task<bool> DeleteBlockState(Guid blockStateId, Guid blockStateMapId)
		{
			var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, blockStateMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			var model = existingMap.Models.Find(x => x.Id == blockStateId);
			if (model == null)
				return false;

			existingMap.Models.Remove(model);

			var update = Builders<BlockStateMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);
			return updated.IsAcknowledged;
		}

		#endregion

		#region Delete
		public async Task<bool> DeleteById(Guid id)
		{
			try
			{
				var filter = Builders<BlockStateMapping>.Filter.Eq(t => t.Id, id);
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

	public interface IBlockStateMapRepository
	{
		// Create
		Task AddBlockStateMap(BlockStateMapping modelMapping);

		// Read
		Task<BlockStateMapping?> GetBlockStateMappingById(Guid id);
		Task<List<BlockStateMapping>> GetBlockStateMappingsByName(string name);
		Task<Dictionary<Guid, string>> GetAllBlockStateMappingIds();
		Task<List<BlockStateMapping>> GetAllBlockStateMappings();
		Task<string> GetBlockStateMappingNameById(Guid id);
		Task<bool> DoesBlockStateExist(BlockState blockState, Guid blockStateMapId);

		// Update
		Task<bool> UpdateNameById(Guid id, string newName);
		Task<bool> AddBlockState(BlockState asset, Guid blockStateMapId);
		Task<bool> EditBlockState(BlockState asset, Guid blockStateId, Guid blockStateMapId);
		Task<bool> DeleteBlockState(Guid blockStateId, Guid blockStateMapId);

		// Delete
		Task<bool> DeleteById(Guid id);
	}
}
