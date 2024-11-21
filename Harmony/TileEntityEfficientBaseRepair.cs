using Audio;
using UnityEngine;
using System.Collections.Generic;
using System;
using static Block;

public class TileEntityEfficientBaseRepair : TileEntitySecureLootContainer //TODO: Implement IPowered interface
{
	/* XML PARAMS */

	public static readonly DynamicProperties properties = new BlockValue((uint)GetBlockByName("EfficientBaseRepair").blockID).Block.Properties;

	public static readonly int maxBfsIterations = properties.GetInt("MaxBfsIterations");

	private static readonly bool needMaterialsForRepair = properties.GetBool("NeedsMaterialsForRepair");

	private static readonly bool needMaterialsForUpgrade = properties.GetBool("NeedsMaterialsForUpgrade");

	private static readonly bool activeDuringBloodMoon = properties.GetBool("ActiveDuringBloodMoon");

	public static readonly int repairRate = properties.GetInt("RepairRate");

	private static readonly int refreshRate = properties.GetInt("RefreshRate");

	public static readonly bool playRepairSound = properties.GetBool("PlayRepairSound");

	private static readonly bool autoTurnOff = properties.GetBool("AutoTurnOff");

	private static readonly int upgradeRate = properties.GetInt("UpgradeRate");

	private static readonly bool keepPaintAfterUpgrade = properties.GetBool("KeepPaintAfterUpgrade");

	/* PUBLIC STATS */

	public int damagedBlockCount = 0;

	public int upgradableBlockCount = 0;

	public int visitedBlocksCount = 0;

	public int bfsIterationsCount = 0;

	public int totalDamagesCount = 0;

	/* CLASS ATTRIBUTES */

	private bool isOn;

	public bool upgradeOn;

	public bool isPowered;

	private bool forceFullRefresh;

	private bool forceRefreshMaterials;

	public bool IsOn => isOn;

	private int elapsedTicksSinceLastRefresh = 0;

	private string UpgradeSound => "nailgun_fire";

	private string RepairSound(BlockValue block) => string.Format("ImpactSurface/metalhit{0}", block.Block.blockMaterial.SurfaceCategory);

	private static World world => GameManager.Instance.World;

	public List<Vector3i> blocksToRepair;

	List<Vector3i> blocksToUpgrade = new List<Vector3i>();

	public Dictionary<string, int> requiredMaterials;

	private List<BlockChangeInfo> blockChangeInfos = new List<BlockChangeInfo>();

	public override TileEntityType GetTileEntityType() => (TileEntityType)191;

	public TileEntityEfficientBaseRepair(Chunk _chunk) : base(_chunk)
	{
		isOn = false;
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

		var itemName = GetUpgradeItemName(block.Block);
		var itemCount = upgradeProperties.GetInt("UpgradeBlock.ItemCount");

		if (itemName is null || itemName == "" || itemCount <= 0)
			return null;

		return new Dictionary<string, int>(){
			{
				itemName,
				itemCount
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
			// can happen if the structure was modified since the last refresh
			if (!requiredMaterials.ContainsKey(entry.Key))
				requiredMaterials[entry.Key] = entry.Value;

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

	private void DamageBlock(int repairAmount, int clrIdx, BlockValue block, Vector3i pos, string audioClipName)
	{
		block.Block.DamageBlock(GameManager.Instance.World, clrIdx, pos, block, repairAmount, 0);

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
		if (!(world.GetChunkFromWorldPos(pos) is Chunk chunk))
		{
			Log.Warning("Can't retreive chunk from world position.");
			return 0;
		}

		BlockValue block = world.GetBlock(pos);
		List<SItemNameCount> repairItems = block.Block.RepairItems;

		int repairableDamages = ComputeRepairableDamages(block, maxRepairableDamages, repairItems);

		if (repairableDamages <= 0)
			return 0;

		totalDamagesCount -= repairableDamages;

		DamageBlock(-repairableDamages, chunk.ClrIdx, block, pos, RepairSound(block));

		return repairableDamages;
	}

	private bool TryUpgradeBlock(World world, Vector3i pos)
	{
		if (!(world.GetChunkFromWorldPos(pos) is Chunk chunk))
			return false;

		Dictionary<string, int> availableMaterials = ItemsToDict();
		Dictionary<string, int> upgradeMaterials = GetUpgradeMaterialsForPos(pos);

		if (upgradeMaterials == null)
			return false;

		foreach (var entry in upgradeMaterials)
		{
			if (!needMaterialsForUpgrade)
				continue;

			if (!availableMaterials.ContainsKey(entry.Key))
				return false;

			int availableItemCount = availableMaterials[entry.Key];
			int requiredItemCount = upgradeMaterials[entry.Key];

			if (availableItemCount < requiredItemCount)
				return false;
		}

		TakeRepairMaterials(upgradeMaterials, needMaterialsForUpgrade);

		BlockValue currentBlock = world.GetBlock(pos);
		Vector3i localPos = World.toBlock(pos);

		var textureFull = chunk.GetTextureFull(localPos.x, localPos.y, localPos.z);

		DamageBlock(-1, chunk.ClrIdx, currentBlock, pos, UpgradeSound);
		SetBlockUpgradable(pos);

		if (keepPaintAfterUpgrade)
		{
			blockChangeInfos.Add(new BlockChangeInfo(
				chunk.ClrIdx,
				pos,
				chunk.GetBlock(localPos.x, localPos.y, localPos.z),
				chunk.GetDensity(localPos.x, localPos.y, localPos.z),
				textureFull
			));
		}

		return true;
	}


	private string GetUpgradeItemName(Block block)
	{
		// NOTE: copied from ItemActionRepair.GetUpgradeItemName()

		string text = block.Properties.Values["UpgradeBlock.Item"];
		if (text != null && text.Length == 1 && text[0] == 'r')
		{
			text = block.RepairItems[0].ItemName;
		}

		return text;
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

				if (block.damage > 0)
				{
					blocksToRepair.Add(pos);
					totalDamagesCount += block.damage;
				}
				else if (block.Block.Properties.Values.ContainsKey("UpgradeBlock.UpgradeHitCount"))
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

		Log.Out($"[EfficientBaseRepair] damagedBlockCount    = {blocksToRepair.Count}");
		Log.Out($"[EfficientBaseRepair] upgradableBlockCount = {blocksToRepair.Count}");
		Log.Out($"[EfficientBaseRepair] Iterations           = {maxBfsIterations - iterations}/{maxBfsIterations}");
		Log.Out($"[EfficientBaseRepair] visited_blocks       = {visited.Count}");
	}

	public void ForceRefresh()
	{
		forceFullRefresh = true;
		setModified();
	}

	private void RefreshStats(World world)
	{
		elapsedTicksSinceLastRefresh = 0;
		damagedBlockCount = 0;
		bfsIterationsCount = 0;
		visitedBlocksCount = 0;
		totalDamagesCount = 0;
		forceFullRefresh = false;

		AnalyseStructure(ToWorldPos());
		RefreshMaterialsStats();
	}

	private void RefreshMaterialsStats()
	{
		forceRefreshMaterials = false;

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
			forceFullRefresh = true;

		isOn = !isOn;
		Logging($"Switch TileEntity to {isOn}");
		Manager.PlayInsidePlayerHead(isOn ? "switch_up" : "switch_down");
		setModified();
	}

	public void SwitchUpgrade()
	{
		upgradeOn = !upgradeOn;
		forceRefreshMaterials = true;
		setModified();
	}

	public override void read(PooledBinaryReader _br, StreamModeRead _eStreamMode)
	{
		base.read(_br, _eStreamMode);
		isOn = _br.ReadBoolean();
		upgradeOn = _br.ReadBoolean();
		isPowered = _br.ReadBoolean();

		if (_eStreamMode == StreamModeRead.Persistency)
			return;

		forceRefreshMaterials = _br.ReadBoolean();
		forceFullRefresh = _br.ReadBoolean();
		damagedBlockCount = _br.ReadInt32();
		totalDamagesCount = _br.ReadInt32();
		visitedBlocksCount = _br.ReadInt32();
		bfsIterationsCount = _br.ReadInt32();
		upgradableBlockCount = _br.ReadInt32();

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
		if (forceFullRefresh)
		{
			Log.Out("[EfficientBaseRepair] Refresh forced from server.");
			RefreshStats(GameManager.Instance.World);
			setModified();
		}
		else if (forceRefreshMaterials)
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
		_bw.Write(isPowered);

		if (_eStreamMode == StreamModeWrite.Persistency)
			return;

		_bw.Write(forceRefreshMaterials);
		_bw.Write(forceFullRefresh);
		_bw.Write(damagedBlockCount);
		_bw.Write(totalDamagesCount);
		_bw.Write(visitedBlocksCount);
		_bw.Write(bfsIterationsCount);
		_bw.Write(upgradableBlockCount);

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

		if (_eStreamMode == StreamModeWrite.ToServer)
			return;

		// trigger forceRefresh=true in single player mode
		// TODO: try with SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer
		if (forceFullRefresh)
		{
			Log.Out("[EfficientBaseRepair] Refresh forced from Client.");
			RefreshStats(GameManager.Instance.World);
		}
		else if (forceRefreshMaterials)
		{
			RefreshMaterialsStats();
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

		if (!upgradeOn)
			return;

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
		int totalRepairedDamages = 0;

		foreach (Vector3i position in new List<Vector3i>(blocksToRepair))
		{
			int repairedDamages = TryRepairBlock(world, position, repairableDamages);

			totalRepairedDamages += repairedDamages;
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

		if (totalRepairedDamages > 0)
			Log.Out($"[EfficientBaseRepair] {totalRepairedDamages} hit points repaired.");

		return totalRepairedDamages > 0;
	}

	private bool UpgradeBlocks(World world)
	{
		if (!upgradeOn)
			return false;

		int upgradeCountTarget = upgradeRate > 0 ? upgradeRate : int.MaxValue;
		int upgradedBlocksCount = 0;

		foreach (Vector3i position in new List<Vector3i>(blocksToUpgrade))
		{
			if (!TryUpgradeBlock(world, position))
				continue;

			upgradableBlockCount--;
			upgradeCountTarget--;
			upgradedBlocksCount++;

			blocksToUpgrade.Remove(position);

			if (upgradeCountTarget == 0)
				break;
		}

		if (upgradedBlocksCount > 0)
			Log.Out($"[EfficientBaseRepair] {upgradedBlocksCount} blocks upgraded.");

		return upgradedBlocksCount > 0;
	}

	public override void UpdateTick(World world)
	{
		base.UpdateTick(world);

		blockChangeInfos.Clear();

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

		if (blockChangeInfos.Count > 0)
		{
			GameManager.Instance.World.SetBlocksRPC(blockChangeInfos);
		}

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
