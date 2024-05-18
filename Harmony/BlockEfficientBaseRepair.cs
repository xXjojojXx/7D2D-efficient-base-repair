using System;
using Audio;
using Platform;
using UnityEngine;

class BlockEfficientBaseRepair : BlockSecureLoot
{
    private Vector2i LootSize = new Vector2i(8, 8);

    private const string TURN_ON_CMD = "turn_repair_on";
    private const string TURN_OFF_CMD = "turn_repair_off";

    private int TakeDelay;

    // copied from ocbClaimAutoRepair and adapted from BlockLandClaim
    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
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
    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
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

        TakeDelay = this.Properties.GetInt("TakeDelay");

        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        TileEntityEfficientBaseRepair tileEntity = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityEfficientBaseRepair;

        Log.Out(tileEntity == null ? "Null TileEntityEfficientBaseRepair": "Not null TileEntityEfficientBaseRepair");

        string cmd_activate = tileEntity.is_activated ? TURN_OFF_CMD : TURN_ON_CMD;
        bool is_locked = tileEntity.IsLocked();

        Log.Out("cmd_activate: " + cmd_activate);

        return new BlockActivationCommand[6]
        {
            new BlockActivationCommand("Search", "search", true),
            new BlockActivationCommand("lock", "lock", !is_locked),
            new BlockActivationCommand("keypad", "keypad", true),
            new BlockActivationCommand("unlock", "unlock", is_locked),
            new BlockActivationCommand(TURN_ON_CMD, "electric_switch", true),
            new BlockActivationCommand("take", "hand", true),
        };

    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return Localization.Get("useWorkstation");
    }

    // copied from BlockSecureLoot
    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {

        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_commandName, _world, _cIdx, parentPos, block, _player);
        }

        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer))
        {
            return false;
        }

        switch (_commandName)
        {
            case TURN_ON_CMD:
                break;

            case TURN_OFF_CMD:
                break;

            case "take":
				TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
				return true;

        }

        return base.OnBlockActivated(_commandName, _world, _cIdx, _blockPos, _blockValue, _player);
    }

    // copied from BlockSecureLoot
    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_world, _cIdx, parentPos, block, _player);
        }

        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer))
        {
            return false;
        }

        _player.AimingGun = false;
        Vector3i blockPos = tileEntitySecureLootContainer.ToWorldPos();
        tileEntitySecureLootContainer.bWasTouched = tileEntitySecureLootContainer.bTouched;
        _world.GetGameManager().TELockServer(_cIdx, blockPos, tileEntitySecureLootContainer.entityId, _player.entityId);
        return true;
    }

    // copied from BlockWorkStation
	private void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
	{
		if (_blockValue.damage > 0)
		{
			GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
			return;
		}
		if (!(GameManager.Instance.World.GetTileEntity(_cIdx, _blockPos) as TileEntityEfficientBaseRepair).IsEmpty())
		{
			GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttWorkstationNotEmpty"), string.Empty, "ui_denied");
			return;
		}
		LocalPlayerUI playerUI = (_player as EntityPlayerLocal).PlayerUI;
		playerUI.windowManager.Open("timer", _bModal: true);
		XUiC_Timer childByType = playerUI.xui.GetChildByType<XUiC_Timer>();
		TimerEventData timerEventData = new TimerEventData();
		timerEventData.Data = new object[4] { _cIdx, _blockValue, _blockPos, _player };
		timerEventData.Event += EventData_Event;
		childByType.SetTimer(TakeDelay, timerEventData);
	}

    // copied from BlockWorkStation
	private void EventData_Event(TimerEventData timerData)
	{
		World world = GameManager.Instance.World;
		object[] obj = (object[])timerData.Data;
		int clrIdx = (int)obj[0];
		BlockValue blockValue = (BlockValue)obj[1];
		Vector3i vector3i = (Vector3i)obj[2];
		BlockValue block = world.GetBlock(vector3i);
		EntityPlayerLocal entityPlayerLocal = obj[3] as EntityPlayerLocal;
		if (block.damage > 0)
		{
			GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
			return;
		}
		if (block.type != blockValue.type)
		{
			GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttBlockMissingPickup"), string.Empty, "ui_denied");
			return;
		}
		TileEntityEfficientBaseRepair tileEntity = world.GetTileEntity(clrIdx, vector3i) as TileEntityEfficientBaseRepair;
		if (tileEntity.IsUserAccessing())
		{
			GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttCantPickupInUse"), string.Empty, "ui_denied");
			return;
		}
		LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
		HandleTakeInternalItems(tileEntity, uIForPlayer);
		ItemStack itemStack = new ItemStack(block.ToItemValue(), 1);
		if (!uIForPlayer.xui.PlayerInventory.AddItem(itemStack))
		{
			uIForPlayer.xui.PlayerInventory.DropItem(itemStack);
		}
		world.SetBlockRPC(clrIdx, vector3i, BlockValue.Air);
	}

    // copied from BlockWorkStation
	protected virtual void HandleTakeInternalItems(TileEntityEfficientBaseRepair te, LocalPlayerUI playerUI)
	{
		ItemStack[] items = te.items;
		for (int i = 0; i < items.Length; i++)
		{
			if (!items[i].IsEmpty() && !playerUI.xui.PlayerInventory.AddItem(items[i]))
			{
				playerUI.xui.PlayerInventory.DropItem(items[i]);
			}
		}
	}

}