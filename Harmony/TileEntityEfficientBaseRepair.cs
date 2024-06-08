using Audio;
using UnityEngine;
using System.Collections.Generic;
using System;
using static Block;

public class TileEntityEfficientBaseRepair : TileEntitySecureLootContainer //TODO: Implement IPowered interface
{
	/* XML PARAMS */
	public int maxBfsIterations;

	private bool needMaterialsForRepair;

	private bool needMaterialsForUpgrade;

	private bool activeDuringBloodMoon;

	public int repairRate;

	private int refreshRate;

	public bool playRepairSound = true;

	private bool autoTurnOff = false;

	private int upgradeRate;

	private bool keepPaintAfterUpgrade;

	/* PUBLIC STATS */

	public int damagedBlockCount = 0;

	public int upgradableBlockCount = 0;

	public int visitedBlocksCount = 0;

	public int bfsIterationsCount = 0;

	public int totalDamagesCount = 0;

	/* CLASS ATTRIBUTES */

	private bool isOn;

	public bool upgradeOn;

	private bool forceRefresh;

	public bool IsOn => isOn;

	private int elapsedTicksSinceLastRefresh = 0;

	private string UpgradeSound => "nailgun_fire";

	private string RepairSound(BlockValue block) => string.Format("ImpactSurface/metalhit{0}", block.Block.blockMaterial.SurfaceCategory);

	private World world;

	public List<Vector3i> blocksToRepair;

	List<Vector3i> blocksToUpgrade = new List<Vector3i>();

	public Dictionary<string, int> requiredMaterials;

	public override TileEntityType GetTileEntityType() => (TileEntityType)191;

	public TileEntityEfficientBaseRepair(Chunk _chunk) : base(_chunk)
	{
		isOn = false;
	}

	private void Init(World _world)
	{
		world = _world;

		DynamicProperties properties = _world.GetBlock(ToWorldPos()).Block.Properties;

		maxBfsIterations = properties.GetInt("MaxBfsIterations");
		needMaterialsForRepair = properties.GetBool("NeedsMaterialsForRepair");
		needMaterialsForUpgrade = properties.GetBool("NeedsMaterialsForUpgrade");
		repairRate = properties.GetInt("RepairRate");
		refreshRate = properties.GetInt("RefreshRate");
		playRepairSound = properties.GetBool("PlayRepairSound");
		activeDuringBloodMoon = properties.GetBool("ActiveDuringBloodMoon");
		autoTurnOff = properties.GetBool("AutoTurnOff");
		upgradeRate = properties.GetInt("UpgradeRate");
		keepPaintAfterUpgrade = properties.GetBool("KeepPaintAfterUpgrade");
	}

	public string RepairTime()
	{
		const float tickDuration_s = 2f;

		float repairTime_s = 0f;
		float upgradeTime_s = 0f;

		if (repairRate > 0)
			repairTime_s = (float)(totalDamagesCount * tickDuration_s) / repairRate;

		if (upgradeRate > 0 && upgradeOn)
			upgradeTime_s = (float)(upgradableBlockCount * tickDuration_s) / upgradeRate;

		return TimeSpan.FromSeconds(repairTime_s + upgradeTime_s).ToString(@"hh\:mm\:ss");
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

	private Dictionary<string, int> GetUpgradeMaterialsForPos(Vector3i pos)
	{
		if (world.GetChunkFromWorldPos(pos) == null)
			return null;

		BlockValue block = world.GetBlock(pos);
		DynamicProperties upgradeProperties = block.Block.Properties;

		if (block.isair || block.isWater || block.ischild)
			return null;

		if (!upgradeProperties.Values.ContainsKey("UpgradeBlock.Item"))
			return null;

		if (upgradeProperties.GetString("UpgradeBlock.Item") == "r")
			return null;

		return new Dictionary<string, int>(){
			{
				upgradeProperties.GetString("UpgradeBlock.Item"),
				upgradeProperties.GetInt("UpgradeBlock.ItemCount")
			}
		};
	}

	private Dictionary<string, int> GetRepairMaterialsForPos(Vector3i pos)
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

	public int TakeRepairMaterials(Dictionary<string, int> materials, bool needMaterials)
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

		Logging($"needMaterials={needMaterialsForRepair}");
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
			if (needMaterialsForRepair && availableItemCount < targetItemCount)
			{
				return 0;
			}

			materialsToTake[item.ItemName] = targetItemCount;
			totalRequired += (int)Mathf.Ceil(item.Count * blockDamagePerc);

			Logging($"targetItemCount={targetItemCount}");
			Logging($"totalRequired={totalRequired}");
		}

		int totalTaken = TakeRepairMaterials(materialsToTake, needMaterialsForRepair);

		float repairedDamages = (float)block.damage * totalTaken / totalRequired;

		Logging($"totalTaken={totalTaken}");
		Logging($"repairedDamages={repairedDamages:F3}");

		return (int)Mathf.Ceil(repairedDamages);
	}

	private void UpdateBlock(Chunk chunkFromWorldPos, BlockValue block, Vector3i pos, string audioClipName)
	{
		// BroadCast the changes done to the block (copied from ocbClaimAutoRepair)
		world.SetBlock(chunkFromWorldPos.ClrIdx, pos, block, false, false);
		world.SetBlockRPC(
			chunkFromWorldPos.ClrIdx,
			pos,
			block,
			block.Block.Density
		);

		if (!playRepairSound || audioClipName == string.Empty)
			return;

		// play material specific sound (copied from ocbClaimAutoRepair)
		world.GetGameManager().PlaySoundAtPositionServer(
			_pos: pos.ToVector3(),
			_audioClipName: audioClipName,
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

		UpdateBlock(chunkFromWorldPos, block, pos, RepairSound(block));

		return repairableDamages;
	}

	private bool TryUpgradeBlock(World world, Vector3i pos)
	{
		if (!(world.GetChunkFromWorldPos(pos) is Chunk chunk))
			return false;

		Dictionary<string, int> availableMaterials = ItemsToDict();
		Dictionary<string, int> requiredMaterials = GetUpgradeMaterialsForPos(pos);

		if (requiredMaterials == null)
			return false;

		foreach (var entry in requiredMaterials)
		{
			if (!needMaterialsForUpgrade)
				continue;

			if (!availableMaterials.ContainsKey(entry.Key))
				return false;

			int availableItemCount = availableMaterials[entry.Key];
			int requiredItemCount = requiredMaterials[entry.Key];

			if (availableItemCount < requiredItemCount)
				return false;
		}

		TakeRepairMaterials(requiredMaterials, needMaterialsForUpgrade);

		BlockValue currentBlock = world.GetBlock(pos);
		BlockValue upgradedBlock = currentBlock.Block.UpgradeBlock;

		upgradedBlock.rotation = currentBlock.rotation;

		if (!keepPaintAfterUpgrade)
			GameManager.Instance.SetBlockTextureServer(pos, BlockFace.None, 0, -1);

		UpdateBlock(chunk, upgradedBlock, pos, UpgradeSound);
		SetBlockUpgradable(pos);

		Log.Out($"[EfficientBaseRepair] Upgrade to {upgradedBlock.Block.GetBlockName()} at pos {pos}");

		return true;
	}

	public void AnalyseStructure(Vector3i initial_pos)
	{
		blocksToRepair = new List<Vector3i>();
		blocksToUpgrade = new List<Vector3i>();

		List<Vector3i> neighbors = GetNeighbors(initial_pos);
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

				bool isIgnored = IsBlockIgnored(block);
				bool isVisited = visited.ContainsKey(pos.ToString());

				if (!isVisited)
					visited.Add(pos.ToString(), 0);

				if (isIgnored || isVisited || block.ischild)
					continue;

				// allow to include damaged spike blocks
				string block_name = block.Block.GetBlockName();

				if (block.damage > 0 || block_name.Contains("Dmg1") || block_name.Contains("Dmg2"))
				{
					blocksToRepair.Add(pos);
					totalDamagesCount += block.damage;
				}
				else if (block.Block.Properties.Values.ContainsKey("UpgradeBlock.Item"))
				{
					blocksToUpgrade.Add(pos);
				}

				neighbors.AddRange(GetNeighbors(pos));
			}
		}

		damagedBlockCount = blocksToRepair.Count;
		bfsIterationsCount = maxBfsIterations - iterations;
		visitedBlocksCount = visited.Count;
		upgradableBlockCount = blocksToUpgrade.Count;

		Log.Out($"[EfficientBaseRepair] {blocksToRepair.Count} blocks to repair. Iterations = {maxBfsIterations - iterations}/{maxBfsIterations}, visited_blocks = {visited.Count}");
	}

	public void ForceRefresh()
	{
		forceRefresh = true;
		setModified();
	}

	private void RefreshStats(World world)
	{
		Init(world);

		elapsedTicksSinceLastRefresh = 0;
		damagedBlockCount = 0;
		bfsIterationsCount = 0;
		visitedBlocksCount = 0;
		totalDamagesCount = 0;
		forceRefresh = false;

		AnalyseStructure(ToWorldPos());
		RefreshMaterialsStats();
	}

	private void RefreshMaterialsStats()
	{
		if (blocksToRepair == null)
			return;

		requiredMaterials = new Dictionary<string, int>();

		foreach (Vector3i position in blocksToRepair)
		{
			Dictionary<string, int> missingMaterials = GetRepairMaterialsForPos(position);

			if (missingMaterials == null)
				continue;

			foreach (KeyValuePair<string, int> entry in missingMaterials)
			{
				if (!requiredMaterials.ContainsKey(entry.Key))
					requiredMaterials.Add(entry.Key, 0);

				requiredMaterials[entry.Key] += entry.Value;
			}
		}

		if (!upgradeOn || blocksToUpgrade == null)
			return;

		foreach (Vector3i position in blocksToUpgrade)
		{
			Dictionary<string, int> missingMaterials = GetUpgradeMaterialsForPos(position);

			if (missingMaterials == null)
				continue;

			foreach (KeyValuePair<string, int> entry in missingMaterials)
			{
				if (!requiredMaterials.ContainsKey(entry.Key))
					requiredMaterials.Add(entry.Key, 0);

				requiredMaterials[entry.Key] += entry.Value;
			}
		}
	}

	public void Switch(bool forceRefresh_ = false)
	{
		if (forceRefresh_)
			forceRefresh = true;

		isOn = !isOn;
		Logging($"Switch TileEntity to {isOn}");
		Manager.PlayInsidePlayerHead(isOn ? "switch_up" : "switch_down");
		setModified();
	}

	public void SwitchUpgrade()
	{
		upgradeOn = !upgradeOn;
		setModified();
	}

	public override void read(PooledBinaryReader _br, StreamModeRead _eStreamMode)
	{
		base.read(_br, _eStreamMode);
		isOn = _br.ReadBoolean();
		upgradeOn = _br.ReadBoolean();

		if (_eStreamMode == StreamModeRead.Persistency)
			return;

		forceRefresh = _br.ReadBoolean();
		damagedBlockCount = _br.ReadInt32();
		totalDamagesCount = _br.ReadInt32();
		visitedBlocksCount = _br.ReadInt32();
		bfsIterationsCount = _br.ReadInt32();
		upgradableBlockCount = _br.ReadInt32();
		upgradeRate = _br.ReadInt32();
		repairRate = _br.ReadInt32();

		// send requiredMaterials from server to client, to update Materials panel.
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

		if (_eStreamMode == StreamModeRead.FromServer)
			return;

		// force refresh on server side if he receives the param forceRefresh=true from client.
		if (forceRefresh)
		{
			Log.Out("[EfficientBaseRepair] Refresh forced from server.");
			RefreshStats(GameManager.Instance.World);
			setModified();
		}
		else
		{
			RefreshMaterialsStats();
			setModified();
		}
	}

	public override void write(PooledBinaryWriter _bw, StreamModeWrite _eStreamMode)
	{
		base.write(_bw, _eStreamMode);
		_bw.Write(isOn);
		_bw.Write(upgradeOn);

		if (_eStreamMode == StreamModeWrite.Persistency)
			return;

		_bw.Write(forceRefresh);
		_bw.Write(damagedBlockCount);
		_bw.Write(totalDamagesCount);
		_bw.Write(visitedBlocksCount);
		_bw.Write(bfsIterationsCount);
		_bw.Write(upgradableBlockCount);
		_bw.Write(upgradeRate);
		_bw.Write(repairRate);

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
			RefreshStats(GameManager.Instance.World);
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

	private void SetBlockUpgradable(Vector3i position)
	{
		Dictionary<string, int> upgradeMaterials = GetUpgradeMaterialsForPos(position);

		if (upgradeMaterials == null)
			return;

		blocksToUpgrade.Add(position);
		upgradableBlockCount++;

		foreach (KeyValuePair<string, int> entry in upgradeMaterials)
		{
			if (!requiredMaterials.ContainsKey(entry.Key))
				requiredMaterials[entry.Key] = 0;

			requiredMaterials[entry.Key] += entry.Value;
		}
	}

	private bool RepairBlocks(World world)
	{
		Logging($"TickRepair, {blocksToRepair.Count} blocks to repair, needMaterials={needMaterialsForRepair}");

		int repairableDamages = repairRate > 0 ? repairRate : int.MaxValue;

		bool wasRepaired = false;

		foreach (Vector3i position in new List<Vector3i>(blocksToRepair))
		{
			int repairedDamages = TryRepairBlock(world, position, repairableDamages);

			wasRepaired |= repairedDamages > 0;

			repairableDamages -= repairedDamages;

			BlockValue block = world.GetBlock(position);

			if (block.damage == 0)
			{
				Logging($"full repaired block at {position}");
				blocksToRepair.Remove(position);
				damagedBlockCount--;
				SetBlockUpgradable(position);
			}

			Logging($"BlockEnd, repairableDamageCount={repairableDamages}\n");

			if (repairableDamages <= 0 && repairRate > 0)
				break;
		}

		return wasRepaired;
	}

	private bool UpgradeBlocks(World world)
	{
		if (!upgradeOn)
			return false;

		int upgradableBlocksCount = upgradeRate > 0 ? upgradeRate : int.MaxValue;
		int upgradedBlocksCount = 0;

		foreach (Vector3i position in new List<Vector3i>(blocksToUpgrade))
		{
			if (!TryUpgradeBlock(world, position))
				continue;

			upgradableBlocksCount--;
			upgradedBlocksCount++;

			blocksToUpgrade.Remove(position);

			if (upgradableBlocksCount == 0)
				break;
		}

		return upgradedBlocksCount > 0;
	}

	public override void UpdateTick(World world)
	{
		base.UpdateTick(world);

		if (!isOn || BloodMoonActive(world))
			return;

		if (blocksToRepair == null || (elapsedTicksSinceLastRefresh >= refreshRate && refreshRate > 0))
			RefreshStats(world);

		if (blocksToRepair == null)
		{
			Log.Warning("[EfficientBaseRepair] TileEntityEfficientBaseRepair.blocksToRepair initializing failed.");
			return;
		}

		bool wasModified = false;

		wasModified |= RepairBlocks(world);
		wasModified |= UpgradeBlocks(world);

		if (wasModified)
		{
			setModified();
		}
		else if (autoTurnOff)
		{
			isOn = false;
			setModified();
		}

		Logging("[EfficientBaseRepair] TickEnd\n\n");
		elapsedTicksSinceLastRefresh++;
	}
}
