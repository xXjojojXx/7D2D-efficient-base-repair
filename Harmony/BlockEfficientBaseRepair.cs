using System;
using UnityEngine;
using System.Collections.Generic;

class BlockEfficientBaseRepair : BlockSecureLoot
{
    private Vector2i LootSize = new Vector2i(8, 8);

    // copied from ocbClaimAutoRepair and adapted from BlockLandClaim
    public override void OnBlockEntityTransformAfterActivated(
        WorldBase _world,
        Vector3i _blockPos,
        int _cIdx,
        BlockValue _blockValue,
        BlockEntityData _ebcd)
    {
        if (_ebcd == null)
            return;

        Chunk chunk = (Chunk)((World)_world).GetChunkFromWorldPos(_blockPos);
        TileEntityEfficientBaseRepair tileEntity = (TileEntityEfficientBaseRepair)_world.GetTileEntity(_cIdx, _blockPos);
        if (tileEntity == null)
        {
            tileEntity = new TileEntityEfficientBaseRepair(chunk);
            if (tileEntity != null)
            {
                tileEntity.localChunkPos = World.toBlock(_blockPos);
                tileEntity.SetContainerSize(LootSize);
                chunk.AddTileEntity(tileEntity);
            }
        }

        if (tileEntity == null)
        {
            Log.Error("Tile Entity EfficientBaseRepair was unable to be created!");
        }

        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
    }


    // copied from ocbClaimAutoRepair
    public override void OnBlockAdded(
        WorldBase _world,
        Chunk _chunk,
        Vector3i _blockPos,
        BlockValue _blockValue)
    {
        if (_blockValue.ischild || _world.GetTileEntity(_chunk.ClrIdx, _blockPos) is TileEntityEfficientBaseRepair)
            return;

        // Overload TileEntity creation (base method should still recognize this)
        TileEntityEfficientBaseRepair tileEntity = new TileEntityEfficientBaseRepair(_chunk)
        {
            localChunkPos = World.toBlock(_blockPos),
            lootListName = lootList,
        };

        tileEntity.SetContainerSize(LootSize);
        _chunk.AddTileEntity(tileEntity);

        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
    }

    // copied from ocbClaimAutoRepair
	public override BlockActivationCommand[] GetBlockActivationCommands(
		WorldBase _world,
		BlockValue _blockValue,
		int _clrIdx,
		Vector3i _blockPos,
		EntityAlive _entityFocusing)
	{
		TileEntityEfficientBaseRepair tileEntity = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityEfficientBaseRepair;
		BlockActivationCommand[] commands = base.GetBlockActivationCommands(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);

		Array.Resize(ref commands, commands.Length + 2);
		commands[commands.Length - 2] = new BlockActivationCommand("take", "hand", false);

		string cmd = tileEntity.is_on ? "turn_repair_off" : "turn_repair_on";
		commands[commands.Length - 1] = new BlockActivationCommand(cmd, "electric_switch", true);

		if (this.CanPickup)
			commands[commands.Length - 2].enabled = true;

		else if ((double)EffectManager.GetValue(PassiveEffects.BlockPickup, _entity: _entityFocusing, tags: _blockValue.Block.Tags) > 0.0)
			commands[commands.Length - 2].enabled = true;

		else
			commands[commands.Length - 2].enabled = false;

		return commands;
	}

    public override bool OnBlockActivated(
        string _commandName,
        WorldBase _world,
        int _cIdx,
        Vector3i _blockPos,
        BlockValue _blockValue,
        EntityAlive _player)
    {
        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntityEfficientBaseRepair tileEntity))
            return false;

        if (_commandName == "take")
        {
            // Copied from vanilla Block::OnBlockActivated
            bool flag = this.CanPickup;
            if ((double)EffectManager.GetValue(PassiveEffects.BlockPickup, _entity: _player, tags: _blockValue.Block.Tags) > 0.0)
                flag = true;

            if (!flag)
                return false;

            if (!_world.CanPickupBlockAt(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer()))
            {
                _player.PlayOneShot("keystone_impact_overlay");
                return false;
            }

            if (_blockValue.damage > 0)
            {
                GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), "ui_denied");
                return false;
            }

            ItemStack itemStack = Block.list[_blockValue.type].OnBlockPickedUp(_world, _cIdx, _blockPos, _blockValue, _player.entityId);
            if (!_player.inventory.CanTakeItem(itemStack) && !_player.bag.CanTakeItem(itemStack))
            {
                GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("xuiInventoryFullForPickup"), "ui_denied");
                return false;
            }

            TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
            return false;

        }
        else if (_commandName == "turn_claimautorep_off" || _commandName == "turn_claimautorep_on")
        {
            tileEntity.is_on = !tileEntity.is_on;

            if (!tileEntity.is_on)
                return true;

            return true;

            Dictionary<string, int> missing_items = tileEntity.FindAndRepairDamagedBlocks(_player.world);

            Log.Out($"{missing_items.Count} missing items: ");

            foreach (KeyValuePair<string, int> entry in missing_items)
            {
                string message = $"Missing materials: {Localization.Get(entry.Key)} x{entry.Value}";

                int item_type = ItemClass.GetItem(entry.Key).type;

                // TODO: find a better way to diplay missing items to the client
                EntityPlayerLocal local_player = _player as EntityPlayerLocal;
                local_player.AddUIHarvestingItem(itemStack: new ItemStack(new ItemValue(item_type), -entry.Value), true);

                Log.Out(message);
            }

            return true;
        }
        else
        {
            return base.OnBlockActivated(_commandName, _world, _cIdx, _blockPos, _blockValue, _player);
        }
    }

    // copied from ocbClaimAutoRepair
	public void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
	{
		if (_blockValue.damage > 0)
		{
			GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), "ui_denied");
		}
		else
		{
			LocalPlayerUI playerUi = (_player as EntityPlayerLocal).PlayerUI;
			playerUi.windowManager.Open("timer", true);
			XUiC_Timer childByType = playerUi.xui.GetChildByType<XUiC_Timer>();
			TimerEventData _eventData = new TimerEventData();
			_eventData.Data = new object[4]
			{
				_cIdx,
				_blockValue,
				_blockPos,
				_player
			};

            int TakeDelay = this.Properties.GetInt("TakeDelay");

			_eventData.Event += new TimerEventHandler(EventData_Event);
			childByType.SetTimer(TakeDelay, _eventData);
		}
	}

    // copied from ocbClaimAutoRepair
	private void EventData_Event(TimerEventData timerData)
	{
		World world = GameManager.Instance.World;
		object[] data = (object[]) timerData.Data;
		int _clrIdx = (int) data[0];
		BlockValue blockValue = (BlockValue) data[1];
		Vector3i vector3i = (Vector3i) data[2];
		BlockValue block = world.GetBlock(vector3i);
		EntityPlayerLocal entityPlayerLocal = data[3] as EntityPlayerLocal;
		if (block.damage > 0)
		{
			GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), "ui_denied");
		}
		else if (block.type != blockValue.type)
		{
			GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttBlockMissingPickup"), "ui_denied");
		}
		else
		{
			TileEntityEfficientBaseRepair tileEntity = world.GetTileEntity(_clrIdx, vector3i) as TileEntityEfficientBaseRepair;
			if (tileEntity.IsUserAccessing())
			{
				GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttCantPickupInUse"), "ui_denied");
			}
			else
			{
				LocalPlayerUI uiForPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
				HandleTakeInternalItems(tileEntity, uiForPlayer);
				ItemStack itemStack = new ItemStack(block.ToItemValue(), 1);

				if (!uiForPlayer.xui.PlayerInventory.AddItem(itemStack))
					uiForPlayer.xui.PlayerInventory.DropItem(itemStack);

				world.SetBlockRPC(_clrIdx, vector3i, BlockValue.Air);
			}
		}
	}

    // copied from ocbClaimAutoRepair
	protected virtual void HandleTakeInternalItems(TileEntityEfficientBaseRepair te, LocalPlayerUI playerUI)
	{
		ItemStack[] items = te.items;
		for (int index = 0; index < items.Length; ++index)
		{
			if (!items[index].IsEmpty() && !playerUI.xui.PlayerInventory.AddItem(items[index]))
				playerUI.xui.PlayerInventory.DropItem(items[index]);
		}
	}



}