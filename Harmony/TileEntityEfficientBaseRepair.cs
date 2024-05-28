using UnityEngine;
using System.Collections.Generic;
using System;
using static Block;

public class TileEntityEfficientBaseRepair : TileEntitySecureLootContainer //TODO: Implement IPowered interface
{
	/* XML PARAMS */
	public int maxBfsIterations;

	private bool needMaterials;

	private bool activeDuringBloodMoon;

	public int repairRate;

	private int refreshRate;

	public bool playRepairSound = true;

	/* PUBLIC STATS */

	public int damagedBlockCount = 0;

	public int visitedBlocksCount = 0;

	public int bfsIterationsCount = 0;

	public int totalDamagesCount = 0;

	/* CLASS ATTRIBUTES */

	private bool isOn;

	private bool forceRefresh;

	public bool IsOn
	{
		get => isOn;
	}

	private int elapsedTicksSinceLastRefresh = 0;

	private World world;

	public List<Vector3i> blocksToRepair;

	public Dictionary<string, int> requiredMaterials;

	public override TileEntityType GetTileEntityType() => (TileEntityType)243;

	public TileEntityEfficientBaseRepair(Chunk _chunk) : base(_chunk)
	{
		isOn = false;
	}

	private void Init(World _world)
	{
		world = _world;

		DynamicProperties properties = _world.GetBlock(ToWorldPos()).Block.Properties;

		maxBfsIterations = properties.GetInt("MaxBfsIterations");
		needMaterials = properties.GetBool("NeedsMaterials");
		repairRate = properties.GetInt("RepairRate");
		refreshRate = properties.GetInt("RefreshRate");
		playRepairSound = properties.GetBool("PlayRepairSound");
		activeDuringBloodMoon = properties.GetBool("ActiveDuringBloodMoon");
	}

	public string RepairTime()
	{
		if (repairRate <= 0)
			return "00:00:00";

		const float tickDuration_s = 2f;
		float repairTime_s = (float)(totalDamagesCount * tickDuration_s) / repairRate;

		return TimeSpan.FromSeconds(repairTime_s).ToString(@"hh\:mm\:ss");
	}

	public Dictionary<string, int> ItemsToDict()
	{
		Dictionary<string, int> itemsDict = new Dictionary<string, int>();

		if (items == null)
			return itemsDict;

		foreach (ItemStack stack in items)
		{

			if (stack.itemValue.ItemClass == null)
				continue;

			string itemName = stack.itemValue.ItemClass.Name;

			if (!itemsDict.ContainsKey(itemName))
				itemsDict[itemName] = 0;

			itemsDict[itemName] += stack.count;
		}

		return itemsDict;
	}

	private List<Vector3i> GetNeighbors(Vector3i pos)
	{
		return new List<Vector3i>
		{
			new Vector3i(pos.x + 1, pos.y, pos.z),
			new Vector3i(pos.x - 1, pos.y, pos.z),

			new Vector3i(pos.x, pos.y + 1, pos.z),
			new Vector3i(pos.x, pos.y - 1, pos.z),

			new Vector3i(pos.x, pos.y, pos.z + 1),
			new Vector3i(pos.x, pos.y, pos.z - 1),

			new Vector3i(pos.x, pos.y + 1, pos.z + 1),
			new Vector3i(pos.x, pos.y - 1, pos.z + 1),
			new Vector3i(pos.x, pos.y + 1, pos.z - 1),
			new Vector3i(pos.x, pos.y - 1, pos.z - 1),

			new Vector3i(pos.x + 1, pos.y, pos.z + 1),
			new Vector3i(pos.x - 1, pos.y, pos.z + 1),
			new Vector3i(pos.x + 1, pos.y, pos.z - 1),
			new Vector3i(pos.x - 1, pos.y, pos.z - 1),

			new Vector3i(pos.x + 1, pos.y + 1, pos.z),
			new Vector3i(pos.x + 1, pos.y - 1, pos.z),
			new Vector3i(pos.x - 1, pos.y + 1, pos.z),
			new Vector3i(pos.x - 1, pos.y - 1, pos.z),

			new Vector3i(pos.x + 1, pos.y + 1, pos.z + 1),
			new Vector3i(pos.x + 1, pos.y - 1, pos.z + 1),
			new Vector3i(pos.x + 1, pos.y + 1, pos.z - 1),
			new Vector3i(pos.x + 1, pos.y - 1, pos.z - 1),

			new Vector3i(pos.x - 1, pos.y + 1, pos.z + 1),
			new Vector3i(pos.x - 1, pos.y - 1, pos.z + 1),
			new Vector3i(pos.x - 1, pos.y + 1, pos.z - 1),
			new Vector3i(pos.x - 1, pos.y - 1, pos.z - 1),
		};
	}

	private bool IsBlockIgnored(BlockValue block)
	{

		if (block.damage > 0)
			return false;

		return (
			block.isair
			|| block.isWater
			|| block.Block.shape.IsTerrain()
			|| block.Block.IsPlant()
			|| block.Block.IsTerrainDecoration
		);
	}

	private Dictionary<string, int> GetMissingMaterialsForPos(Vector3i pos)
	{
		if (world.GetChunkFromWorldPos(pos) == null)
			return null;

		BlockValue block = world.GetBlock(pos);

		if (!(block.Block.RepairItems is List<SItemNameCount> repair_items))
			return null;

		Dictionary<string, int> missing_materials = new Dictionary<string, int>();
		float damage_perc = (float)block.damage / block.Block.MaxDamage;

		foreach (SItemNameCount item in repair_items)
		{
			int required_item_count = (int)Mathf.Ceil(item.Count * damage_perc);

			missing_materials.Add(item.ItemName, required_item_count);
		}

		return missing_materials.Count > 0 ? missing_materials : null;
	}

	private int TakeRepairMaterial(string item_name, int itemCount)
	{
		// TODO: optimize this function, by caching 'this.items' in a Hashed structure
		// -> purpose: prevents from iterating over each 'this.items'

		int neededItemCount = itemCount;
		int totalTaken = 0;

		for (int i = 0; i < this.items.Length; i++)
		{
			ItemStack stack = this.items[i];

			if (stack.IsEmpty())
				continue;

			// TODO: see ItemClass.GetItemClass to get an idea of how expensive is this call for `GetItem(string)`
			// TODO: check if `stack.itemValue.ItemClass.Name` can do the job in a more efficient way
			if (stack.itemValue.type != ItemClass.GetItem(item_name).type)
				continue;

			int takenItemsCount = Math.Min(stack.count, neededItemCount);

			neededItemCount -= takenItemsCount;
			stack.count -= takenItemsCount;
			requiredMaterials[item_name] -= takenItemsCount;

			totalTaken += takenItemsCount;

			if (neededItemCount < 0)
				Log.Error($"[EfficientBaseRepair] needed_item_count < 0 (={neededItemCount})");

			if (stack.count < 0)
				Log.Error($"[EfficientBaseRepair] stack.count  < 0 (={stack.count})");

			UpdateSlot(i, stack.Clone());

			if (neededItemCount == 0)
				break;
		}

		Logging($"[EfficientBaseRepair] taking {totalTaken} {item_name}");

		return totalTaken;
	}

	public int TakeRepairMaterials(Dictionary<string, int> materials)
	{
		int totalTaken = 0;

		foreach (KeyValuePair<string, int> entry in materials)
		{
			if (needMaterials)
			{
				totalTaken += TakeRepairMaterial(entry.Key, entry.Value);
			}
			else
			{
				totalTaken += entry.Value;
				requiredMaterials[entry.Key] -= entry.Value;
			}
		}

		return totalTaken;
	}

	private static void Logging(string message)
	{
		// Log.Out($"[EfficientBaseRepair] {message}");
	}

	private int ComputeRepairableDamages(BlockValue block, int maxRepairableDamages, List<SItemNameCount> repairItems)
	{
		int targetRepairedDamages = Mathf.Min(block.damage, maxRepairableDamages);

		Logging($"block.damage={block.damage}");
		Logging($"block.Block.MaxDamage={block.Block.MaxDamage}");
		Logging($"maxRepairableDamages={maxRepairableDamages}");
		Logging($"targetRepairedDamages={targetRepairedDamages}");

		// repair for free of blocks which can't be repaired in vanilla game
		// TODO: add param ForceNonRepairableBlocks
		// TODO: if (block.ischild) ...
		if (repairItems == null)
			return targetRepairedDamages;

		float targetRepairPerc = (float)targetRepairedDamages / block.Block.MaxDamage;
		float blockDamagePerc = (float)block.damage / block.Block.MaxDamage;

		Logging($"targetRepairPerc={targetRepairPerc:F3}");
		Logging($"blockDamagePerc={blockDamagePerc:F3}");

		int totalRequired = 0;

		Dictionary<string, int> itemsDict = ItemsToDict();
		Dictionary<string, int> materialsToTake = new Dictionary<string, int>();

		foreach (SItemNameCount item in repairItems)
		{
			int targetItemCount = (int)Mathf.Ceil(item.Count * targetRepairPerc);
			int availableItemCount = itemsDict.TryGetValue(item.ItemName, out availableItemCount) ? availableItemCount : 0;

			// stop trying to repair the block if one material is missing
			if (availableItemCount < targetItemCount && needMaterials)
			{
				return 0;
			}

			materialsToTake[item.ItemName] = targetItemCount;
			totalRequired += (int)Mathf.Ceil(item.Count * blockDamagePerc);

			Logging($"targetItemCount={targetItemCount}");
			Logging($"totalRequired={totalRequired}");
		}

		int totalTaken = TakeRepairMaterials(materialsToTake);

		float repairedDamages = (float)block.damage * totalTaken / totalRequired;

		Logging($"totalTaken={totalTaken}");
		Logging($"repairedDamages={repairedDamages:F3}");

		return (int)Mathf.Ceil(repairedDamages);
	}

	private void UpdateBlock(Chunk chunkFromWorldPos, BlockValue block, Vector3i pos)
	{
		// BroadCast the changes done to the block (copied from ocbClaimAutoRepair)
		world.SetBlock(chunkFromWorldPos.ClrIdx, pos, block, false, false);
		world.SetBlockRPC(
			chunkFromWorldPos.ClrIdx,
			pos,
			block,
			block.Block.Density
		);

		if (!playRepairSound)
			return;

		// play material specific sound (copied from ocbClaimAutoRepair)
		world.GetGameManager().PlaySoundAtPositionServer(
			_pos: pos.ToVector3(),
			_audioClipName: string.Format("ImpactSurface/metalhit{0}", block.Block.blockMaterial.SurfaceCategory),
			_mode: AudioRolloffMode.Logarithmic,
			_distance: 100
		);
	}

	private int TryRepairBlock(World world, Vector3i pos, int maxRepairableDamages)
	{
		if (!(world.GetChunkFromWorldPos(pos) is Chunk chunkFromWorldPos))
		{
			Log.Warning("Can't retreive chunk from world position.");
			return 0;
		}

		BlockValue block = world.GetBlock(pos);
		List<SItemNameCount> repairItems = block.Block.RepairItems;

		const uint trapSpikesWoodDmg0_id = 21469;
		const uint trapSpikesIronDmg0_id = 21476;

		// handle repairing of spike blocks
		switch (block.Block.GetBlockName())
		{
			case "trapSpikesWoodDmg1":
			case "trapSpikesWoodDmg2":
				block = new BlockValue(trapSpikesWoodDmg0_id);
				break;

			case "trapSpikesIronDmg1":
			case "trapSpikesIronDmg2":
				block = new BlockValue(trapSpikesIronDmg0_id);
				break;

			default:
				// Do nothing -> block = block...
				break;
		}

		int repairableDamages = ComputeRepairableDamages(block, maxRepairableDamages, repairItems);

		if (repairableDamages <= 0)
			return 0;

		block.damage -= repairableDamages;
		totalDamagesCount -= repairableDamages;

		Log.Out($"[EfficientBaseRepair] {repairableDamages} damage points repaired on block {pos}");

		UpdateBlock(chunkFromWorldPos, block, pos);

		return repairableDamages;
	}

	public List<Vector3i> GetBlocksToRepair(Vector3i initial_pos)
	{
		List<Vector3i> blocks_to_repair = new List<Vector3i>();
		List<Vector3i> neighbors = this.GetNeighbors(initial_pos);
		Dictionary<string, int> visited = new Dictionary<string, int>();

		int iterations = maxBfsIterations;

		while (neighbors.Count > 0 && iterations > 0)
		{
			iterations--;

			List<Vector3i> neighbors_temp = new List<Vector3i>(neighbors);
			neighbors = new List<Vector3i>();

			foreach (Vector3i pos in neighbors_temp)
			{
				BlockValue block = world.GetBlock(pos);

				bool is_ignored = this.IsBlockIgnored(block);
				bool is_visited = visited.ContainsKey(pos.ToString());

				if (!is_visited)
					visited.Add(pos.ToString(), 0);

				if (is_ignored || is_visited)
					continue;

				// allow to include damaged spike blocks
				string block_name = block.Block.GetBlockName();

				if (block.damage > 0 || block_name.Contains("Dmg1") || block_name.Contains("Dmg2"))
				{
					blocks_to_repair.Add(pos);
					totalDamagesCount += block.damage;
				}

				neighbors.AddRange(this.GetNeighbors(pos));
			}
		}

		damagedBlockCount = blocks_to_repair.Count;
		bfsIterationsCount = maxBfsIterations - iterations;
		visitedBlocksCount = visited.Count;

		Log.Out($"[EfficientBaseRepair] {blocks_to_repair.Count} blocks to repair. Iterations = {maxBfsIterations - iterations}/{maxBfsIterations}, visited_blocks = {visited.Count}");

		return blocks_to_repair;
	}

	public void Refresh()
	{
		forceRefresh = true;
		setModified();
	}

	private void UpdateStats(World world)
	{
		Init(world);

		damagedBlockCount = 0;
		bfsIterationsCount = 0;
		visitedBlocksCount = 0;
		totalDamagesCount = 0;
		forceRefresh = false;

		Vector3i block_position = ToWorldPos();

		blocksToRepair = GetBlocksToRepair(block_position);

		requiredMaterials = new Dictionary<string, int>();

		foreach (Vector3i position in blocksToRepair)
		{
			Dictionary<string, int> missingMaterials = GetMissingMaterialsForPos(position);

			if (missingMaterials == null)
				continue;

			foreach (KeyValuePair<string, int> entry in missingMaterials)
			{
				if (!requiredMaterials.ContainsKey(entry.Key))
					requiredMaterials.Add(entry.Key, 0);

				requiredMaterials[entry.Key] += entry.Value;
			}
		}

		elapsedTicksSinceLastRefresh = 0;
		setModified();
	}

	public void Switch(bool forceRefresh_ = false)
	{
		if (forceRefresh_)
			forceRefresh = true;

		isOn = !isOn;
		Logging($"Switch TileEntity to {isOn}");
		setModified();
	}

	public override void read(PooledBinaryReader _br, StreamModeRead _eStreamMode)
	{
		base.read(_br, _eStreamMode);

		isOn = _br.ReadBoolean();
		forceRefresh = _br.ReadBoolean();
		damagedBlockCount = _br.ReadInt32();
		totalDamagesCount = _br.ReadInt32();
		visitedBlocksCount = _br.ReadInt32();
		bfsIterationsCount = _br.ReadInt32();

		requiredMaterials = new Dictionary<string, int>();

		int requiredMaterialsCount = _br.ReadInt32();
		if (requiredMaterialsCount > 0)
		{
			for (int i = 0; i < requiredMaterialsCount; i++)
			{
				string itemName = _br.ReadString();
				int itemCount = _br.ReadInt32();

				requiredMaterials[itemName] = itemCount;
			}
		}

		// NOTE: reading again items allows to bypass the bUserAccessing condition from base-classes
		// -> allows the TileEntity to take items from containers, even if the user is accessing the container
		// -> /!\ may cause unknown issues on concurency access to the container
		int itemsCount = _br.ReadInt32();
		if (itemsCount > 0)
		{
			for (int i = 0; i < itemsCount; i++)
			{
				items[i].Read(_br);
			}
		}

		// force refresh on server side if he receives the param forceRefresh=true from client.
		if (_eStreamMode == StreamModeRead.FromClient && forceRefresh)
		{
			Log.Out("[EfficientBaseRepair] Refresh forced from server.");
			UpdateStats(GameManager.Instance.World);
		}
	}

	public override void write(PooledBinaryWriter _bw, StreamModeWrite _eStreamMode)
	{
		base.write(_bw, _eStreamMode);

		_bw.Write(isOn);
		_bw.Write(forceRefresh);
		_bw.Write(damagedBlockCount);
		_bw.Write(totalDamagesCount);
		_bw.Write(visitedBlocksCount);
		_bw.Write(bfsIterationsCount);

		if (requiredMaterials == null)
			requiredMaterials = new Dictionary<string, int>();

		_bw.Write(requiredMaterials.Count);
		foreach (KeyValuePair<string, int> entry in requiredMaterials)
		{
			_bw.Write(entry.Key);
			_bw.Write(entry.Value);
		}

		// see the note in the read method upper
		_bw.Write(items.Length);
		foreach (ItemStack stack in items)
		{
			stack.Clone().Write(_bw);
		}

		// trigger forceRefresh=true in single player mode
		// TODO: try with SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer
		if (_eStreamMode == StreamModeWrite.ToClient && forceRefresh)
		{
			Log.Out("[EfficientBaseRepair] Refresh forced from Client.");
			UpdateStats(GameManager.Instance.World);
		}
	}

	public bool BloodMoonActive(World _world)
	{
		if (activeDuringBloodMoon)
			return false;

		if (_world.aiDirector == null)
			return false;

		bool bloodMoonActive = _world.aiDirector.BloodMoonComponent.BloodMoonActive;

		if (bloodMoonActive && isOn)
			Switch();

		return bloodMoonActive;
	}

	public override void UpdateTick(World world)
	{
		base.UpdateTick(world);

		if (!isOn || BloodMoonActive(world))
		{
			Logging($"TileEntity OFF");
			return;
		}

		if (blocksToRepair == null || (elapsedTicksSinceLastRefresh >= refreshRate && refreshRate > 0))
			UpdateStats(world);

		if (blocksToRepair == null)
		{
			Log.Warning("[EfficientBaseRepair] TileEntityEfficientBaseRepair.blocksToRepair initializing failed.");
			return;
		}

		Logging($"TickRepair, {blocksToRepair.Count} blocks to repair, needMaterials={needMaterials}");

		int repairableDamages = repairRate > 0 ? repairRate : int.MaxValue;

		bool wasModified = false;

		foreach (Vector3i position in new List<Vector3i>(blocksToRepair))
		{
			int repairedDamages = TryRepairBlock(world, position, repairableDamages);

			wasModified |= repairedDamages > 0;

			repairableDamages -= repairedDamages;

			BlockValue block = world.GetBlock(position);

			if (block.damage == 0)
			{
				Logging($"full repaired block at {position}");
				blocksToRepair.Remove(position);
				damagedBlockCount--;
			}

			Logging($"BlockEnd, repairableDamageCount={repairableDamages}\n");

			if (repairableDamages <= 0 && repairRate > 0)
				break;
		}

		elapsedTicksSinceLastRefresh++;

		if (wasModified)
			setModified();

		Logging("[EfficientBaseRepair] TickEnd\n\n");
	}
}
