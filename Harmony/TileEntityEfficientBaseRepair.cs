using UnityEngine;
using System.Collections.Generic;
using System;
using static Block;

public class TileEntityEfficientBaseRepair : TileEntitySecureLootContainer
{
	public bool IsOn;

	private World world;

	/* SHARED ATTRIBUTES */
	public List<Vector3i> blocksToRepair;

	public Dictionary<string, int> requiredMaterials;

	/* XML PARAMS */
	public int maxBfsIterations;

	public int repairPerTick;

	private bool needMaterials;

	public int blocksToRepairCount;

	public int visitedBlocksCount;

	public int bfsIterationsCount;

	public int totalDamagesCount;

	public TileEntityEfficientBaseRepair(Chunk _chunk) : base(_chunk)
	{
		IsOn = false;
	}

	public override TileEntityType GetTileEntityType() => (TileEntityType)243;

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
			//|| block.Block.IsDecoration
			|| block.Block.IsPlant()
			|| block.Block.IsTerrainDecoration
		);
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

		blocksToRepairCount = blocks_to_repair.Count;
		bfsIterationsCount = maxBfsIterations - iterations;
		visitedBlocksCount = visited.Count;

		Log.Out($"[EfficientBaseRepair] {blocksToRepairCount} blocks to repair. Iterations = {maxBfsIterations - iterations}/{maxBfsIterations}, visited_blocks = {visited.Count}");

		return blocks_to_repair;
	}

	private Dictionary<string, int> ComputeRepairMaterials(float damages_perc, List<SItemNameCount> repair_items)
	{
		if (repair_items == null)
			return null;

		Dictionary<string, int> missing_materials = new Dictionary<string, int>();

		foreach (SItemNameCount item in repair_items)
		{
			int required_item_count = (int)Mathf.Ceil(item.Count * damages_perc);

			missing_materials.Add(item.ItemName, required_item_count);
		}

		return missing_materials.Count > 0 ? missing_materials : null;
	}

	private Dictionary<string, int> GetMissingMaterialsForPos(Vector3i pos)
	{
		if (!(world.GetChunkFromWorldPos(pos) is Chunk) || !needMaterials)
		{
			return null;
		}

		// TODO: find a better way to compute the needed repair_items for spike blocks
		// (for now, if a spike is at stage Dmg1 or Dmg2 with damage=0, the upgrade to Dmg0 is free)
		BlockValue block = world.GetBlock(pos);
		List<SItemNameCount> repair_items = block.Block.RepairItems;

		const int trapSpikesWoodDmg0_id = 21469;
		const int trapSpikesIronDmg0_id = 21476;

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

		float damage_perc = (float)block.damage / block.Block.MaxDamage;

		Dictionary<string, int> missing_items = ComputeRepairMaterials(damage_perc, repair_items);

		return missing_items;
	}

	public int ReduceItemCount(string item_name, int item_count)
	{
		// TODO: optimize this function, by caching 'this.items' in a Hashed structure
		// -> purpose: prevents from iterating over each 'this.items'

		int needed_item_count = item_count;

		for (int i = 0; i < this.items.Length; i++)
		{
			ItemStack stack = this.items[i];

			if (stack.IsEmpty())
				continue;

			// TODO: how expensive is this call for `GetItem(string)`? (see ItemClass.GetItemClass to get an idea)
			// TODO: check if this attribute can do the job in a more efficient way: stack.itemValue.ItemClass.Name
			if (stack.itemValue.type != ItemClass.GetItem(item_name).type)
				continue;

			int taken_items_count = Math.Min(stack.count, needed_item_count);

			needed_item_count -= taken_items_count;
			stack.count -= taken_items_count;
			requiredMaterials[item_name] -= taken_items_count;

			if (needed_item_count < 0)
				Log.Error($"[EfficientBaseRepair::ReduceItemCount] needed_item_count < 0 (={needed_item_count})");

			if (stack.count < 0)
				Log.Error($"[EfficientBaseRepair::ReduceItemCount] stack.count  < 0 (={stack.count})");

			UpdateSlot(i, stack);

			if (needed_item_count == 0)
				break;
		}

		return needed_item_count;
	}

	public void Init(World _world, int _max_iterations, bool _need_materials, int _repairPerTick)
	{
		world = _world;
		maxBfsIterations = _max_iterations;
		needMaterials = _need_materials;
		repairPerTick = _repairPerTick;
	}

	private int ComputeRepairableDamages(BlockValue block, float damagePerc, Dictionary<string, int> missing_materials)
	{
		if (block.Block.RepairItems == null)
			return 0;

		if (missing_materials == null)
			return block.damage;

		float total_required = 0.0f;
		float total_missing = 0.0f;

		foreach (SItemNameCount item in block.Block.RepairItems)
		{
			total_required += Mathf.Ceil(item.Count * damagePerc);

			if (!missing_materials.ContainsKey(item.ItemName))
				continue;

			Log.Out($"[ComputeRepairableDamages] missing[{item.ItemName}]={missing_materials[item.ItemName]}");

			total_missing += missing_materials[item.ItemName];
		}

		int damagesAfterRepair = (int)Mathf.Ceil((float)(block.damage * total_missing) / total_required);

		Log.Out($"[ComputeRepairableDamages] {block.Block.GetBlockName()}");
		Log.Out($"[ComputeRepairableDamages] {block.damage} * {total_missing} / {total_required} = {damagesAfterRepair}");
		Log.Out($"[ComputeRepairableDamages] {block.damage} - {damagesAfterRepair} = {block.damage - damagesAfterRepair}");
		Log.Out($"[ComputeRepairableDamages] missingMaterials=[{string.Join(" ", missing_materials.Keys)}]");

		return block.damage - damagesAfterRepair;
	}

	public Dictionary<string, int> TakeRepairMaterials(float damages_perc, List<SItemNameCount> repair_items)
	{
		if (repair_items == null)
			return null;

		Dictionary<string, int> missing_materials = new Dictionary<string, int>();

		foreach (SItemNameCount item in repair_items)
		{
			int required_item_count = (int) Mathf.Ceil(item.Count * damages_perc);
			int missing_item_count = ReduceItemCount(item.ItemName, required_item_count);

			//Log.Out($"{item.ItemName}: required_material={required_item_count} (={item.Count} * {damages_perc:F3})");

			if (missing_item_count > 0)
				missing_materials.Add(item.ItemName, missing_item_count);
		}

		return missing_materials.Count > 0 ? missing_materials : null;
	}

	private int TryRepairBlock(World world, Vector3i pos, int maxRepairableDamages)
	{
		BlockValue block = world.GetBlock(pos);

		if (!(world.GetChunkFromWorldPos(pos) is Chunk chunkFromWorldPos))
		{
			Log.Warning("Can't retreive chunk from world position.");
			return 0;
		}

		// TODO: find a better way to compute the needed repair_items for spike blocks
		// (for now, if a spike is at stage Dmg1 or Dmg2 with damage=0, the upgrade to Dmg0 is free)
		List<SItemNameCount> repair_items = block.Block.RepairItems;

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

		float damagePerc = (float)block.damage / block.Block.MaxDamage;

		Dictionary<string, int> missing_items = needMaterials ? TakeRepairMaterials(damagePerc, repair_items) : null;

		int repairedDamages = Math.Min(maxRepairableDamages, ComputeRepairableDamages(block, damagePerc, missing_items));

		Log.Out($"[TryRepairBlock] repairedDamages={repairedDamages}");

		block.damage -= repairedDamages;

		// BroadCast the changes done to the block (copied from ocbClaimAutoRepair)
		world.SetBlock(chunkFromWorldPos.ClrIdx, pos, block, false, false);
		world.SetBlockRPC(
			chunkFromWorldPos.ClrIdx,
			pos,
			block,
			block.Block.Density
		);

		// play material specific sound (copied from ocbClaimAutoRepair)
		world.GetGameManager().PlaySoundAtPositionServer(
			pos.ToVector3(),
			string.Format("ImpactSurface/metalhit{0}", block.Block.blockMaterial.SurfaceCategory),
			AudioRolloffMode.Logarithmic, 100
		);

		// Update clients (copied from ocbClaimAutoRepair)
		SetModified();

		return repairedDamages;
	}

	public void UpdateStats()
	{

		blocksToRepairCount = 0;
		bfsIterationsCount = 0;
		visitedBlocksCount = 0;
		totalDamagesCount = 0;

		Vector3i block_position = ToWorldPos();

		blocksToRepair = GetBlocksToRepair(block_position);

		requiredMaterials = new Dictionary<string, int>();

		foreach (Vector3i position in blocksToRepair)
		{
			Dictionary<string, int> blockMissingItems = GetMissingMaterialsForPos(position);

			if (blockMissingItems == null)
				continue;

			foreach (KeyValuePair<string, int> entry in blockMissingItems)
			{
				if (!requiredMaterials.ContainsKey(entry.Key))
					requiredMaterials.Add(entry.Key, 0);

				requiredMaterials[entry.Key] += entry.Value;
			}
		}
	}

	public override void UpdateTick(World world)
	{
		base.UpdateTick(world);

		if (!IsOn)
			return;

		if (blocksToRepair.Count == 0)
		{
			IsOn = false;
			return;
		}

		int repairableDamageCount = repairPerTick;

		Log.Out($"\n[EfficientBaseRepair] TickRepair");

		foreach (Vector3i position in new List<Vector3i>(blocksToRepair))
		{

			int repairedDamages = TryRepairBlock(world, position, repairableDamageCount);

			Log.Out($"[EfficientBaseRepair] repaired damages={repairedDamages}, repairableDamages={repairableDamageCount}");

			repairableDamageCount -= repairedDamages;

			BlockValue block = world.GetBlock(position);

			if (block.damage == 0)
				blocksToRepair.Remove(position);

			if (repairableDamageCount == 0) return;
		}
	}
}
