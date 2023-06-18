﻿using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Mappings;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Logic
{
	public class BlockStateLogic : IBlockStateLogic
	{
		private readonly IBlockStateMapRepository _blockStateMapRepository;
		private readonly IPackRepository _packRepository;

		public BlockStateLogic(IBlockStateMapRepository blockStateMapRepository, IPackRepository packRepository)
		{
			_blockStateMapRepository = blockStateMapRepository;
			_packRepository = packRepository;
		}

		public async Task<bool> AddBlockState(string blockStateName, List<Guid> packIds, string fileName, byte[] blockStateFile, MinecraftVersion minVersion, MinecraftVersion? maxVersion)
		{
			List<Pack?> packs = new();
			foreach (var packId in packIds)
				packs.Add(await _packRepository.GetPackById(packId));

			foreach (var pack in packs)
			{
				if (pack?.BlockStateMappingsId == null)
					continue;

				BlockStateMapping? mapping = await _blockStateMapRepository.GetBlockStateMappingById(pack.BlockStateMappingsId.Value);
				if (mapping == null)
					continue;

				BlockState newBlockState = new()
				{
					Id = Guid.NewGuid(),
					Name = blockStateName,
					FileName = fileName,
					Data = blockStateFile,
					MCVersion = new MCVersion()
					{
						MinVersion = minVersion,
						MaxVersion = maxVersion
					}
				};

				bool doesExist = await _blockStateMapRepository.DoesBlockStateExist(newBlockState, mapping.Id);
				if (doesExist)
					return false;

				await _blockStateMapRepository.AddBlockState(newBlockState, mapping.Id);
			}
			return true;
		}

		public async Task<List<BlockState>> SearchForBlockStates(Guid packId, string searchQuery)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack?.BlockStateMappingsId == null)
				return new List<BlockState>();

			BlockStateMapping? map = await _blockStateMapRepository.GetBlockStateMappingById(pack.BlockStateMappingsId.Value);
			return map == null ? new List<BlockState>() : BlockStateSearch(map, searchQuery);
		}

		private List<BlockState> BlockStateSearch(BlockStateMapping map, string searchQuery)
		{
			BlockState? exactNameMatch = map.Models.Find(x => string.Equals(x.Name, searchQuery.ToLower().Trim(), StringComparison.Ordinal));
			List<BlockState> nameMatch = exactNameMatch != null
				? new List<BlockState> { exactNameMatch }
				: map.Models.FindAll(x => x.Name.Contains($"\\{searchQuery}"));

			if (nameMatch.Count > 0)
				return nameMatch;

			BlockState? exactFileNameMatch = map.Models.Find(x => string.Equals(x.FileName, searchQuery.ToLower().Trim(), StringComparison.Ordinal));
			List<BlockState> fileNameMatch = exactFileNameMatch != null
				? new List<BlockState> { exactFileNameMatch }
				: map.Models.FindAll(x => x.FileName.Contains($"\\{searchQuery}"));

			return fileNameMatch;
		}

		public async Task<BlockState?> GetBlockState(Guid packId, string name, MinecraftVersion version)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack?.BlockStateMappingsId == null)
				return null;

			BlockStateMapping? map = await _blockStateMapRepository.GetBlockStateMappingById(pack.BlockStateMappingsId.Value);
			BlockState? blockState = map?.Models.FirstOrDefault(x => x.FileName == name.ToUpper() && x.MCVersion.IsMatchingVersion(version));
			return blockState;
		}

		public async Task<bool> DeleteAllBlockStates(Guid mappingId)
			=> await _blockStateMapRepository.ClearBlockStates(mappingId);
	}

	public interface IBlockStateLogic
	{
		Task<bool> AddBlockState(string blockStateName, List<Guid> packIds, string fileName, byte[] blockStateFile, MinecraftVersion minVersion, MinecraftVersion? maxVersion);
		Task<List<BlockState>> SearchForBlockStates(Guid packId, string searchQuery);
		Task<BlockState?> GetBlockState(Guid packId, string name, MinecraftVersion version);
		Task<bool> DeleteAllBlockStates(Guid mappingId);
	}
}
