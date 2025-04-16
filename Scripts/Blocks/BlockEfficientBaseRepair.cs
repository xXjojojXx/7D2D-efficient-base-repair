using System;
using Audio;
using Platform;
using UnityEngine;

class BlockEfficientBaseRepair : BlockSecureLoot
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<BlockEfficientBaseRepair>();

    private Vector2i LootSize => Config.lootSize;

    private const string TURN_ON_CMD = "EfficientBaseRepairTurnOn";
    private const string TURN_OFF_CMD = "EfficientBaseRepairTurnOff";

    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);

        if (_blockValue.ischild || _world.GetTileEntity(_chunk.ClrIdx, _blockPos) is TileEntityEfficientBaseRepair)
            return;

        TileEntityEfficientBaseRepair tileEntity = new TileEntityEfficientBaseRepair(_chunk)
        {
            localChunkPos = World.toBlock(_blockPos),
            lootListName = lootList
        };

        tileEntity.SetContainerSize(LootSize);
        _chunk.AddTileEntity(tileEntity);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (!(_world.GetTileEntity(_clrIdx, _blockPos) is TileEntityEfficientBaseRepair tileEntity))
        {
            return Array.Empty<BlockActivationCommand>();
        }

        string cmd_activate = tileEntity.IsOn ? TURN_OFF_CMD : TURN_ON_CMD;
        bool is_locked = tileEntity.IsLocked();

        return new BlockActivationCommand[6]
        {
            new BlockActivationCommand("Search", "search", true),
            new BlockActivationCommand("lock", "lock", !is_locked),
            new BlockActivationCommand("unlock", "unlock", is_locked),
            new BlockActivationCommand("keypad", "keypad", true),
            new BlockActivationCommand("take", "hand", true),
            new BlockActivationCommand(cmd_activate, "electric_switch", true, tileEntity.IsOn),
        };

    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return Localization.Get("useWorkstation");
    }

    private void BloodMoonDenied(EntityPlayerLocal _player)
    {
        GameManager.ShowTooltip(_player, Localization.Get("EfficientBaseRepairBloodMoonDenied"), string.Empty, "ui_denied");
    }

    // copied from BlockSecureLoot
    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {

        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_commandName, _world, _cIdx, parentPos, block, _player);
        }

        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntityEfficientBaseRepair tileEntity))
            return false;

        switch (_commandName)
        {
            case TURN_ON_CMD:
            case TURN_OFF_CMD:
                if (tileEntity.BloodMoonActive(_world as World))
                {
                    BloodMoonDenied(_player as EntityPlayerLocal);
                    return false;
                }

                tileEntity.Switch(forceRefresh_: _commandName == TURN_ON_CMD);
                return true;

            case "take":
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;

            case "lock":
                tileEntity.SetLocked(true);
                Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/locking");
                GameManager.ShowTooltip(_player as EntityPlayerLocal, "containerLocked");
                return true;

            case "unlock":
                tileEntity.SetLocked(false);
                Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
                GameManager.ShowTooltip(_player as EntityPlayerLocal, "containerUnlocked");
                return true;

            case "keypad":
                {
                    LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(_player as EntityPlayerLocal);
                    if (uIForPlayer != null)
                    {
                        XUiC_KeypadWindow.Open(uIForPlayer, tileEntity);
                    }

                    return true;
                }

            case "Search":

                bool isLocked = tileEntity.IsLocked();
                bool isAllowed = tileEntity.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
                bool isPlaytesting = GameUtils.IsPlaytesting();

                if (!isLocked || isAllowed || isPlaytesting)
                {
                    return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
                }

                Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/locked");
                return false;

            default:
                return false;

        }
    }

    // copied from BlockSecureLoot
    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_world, _cIdx, parentPos, block, _player);
        }

        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntityEfficientBaseRepair tileEntity))
        {
            return false;
        }

        LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(_player as EntityPlayerLocal);
        if (uIForPlayer == null)
        {
            return false;
        }

        if (tileEntity.BloodMoonActive(_world as World))
        {
            BloodMoonDenied(_player as EntityPlayerLocal);
            return false;
        }

        _player.AimingGun = false;
        Vector3i blockPos = tileEntity.ToWorldPos();
        tileEntity.bWasTouched = tileEntity.bTouched;
        _world.GetGameManager().TELockServer(_cIdx, blockPos, tileEntity.entityId, _player.entityId);
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

        childByType.SetTimer(Properties.GetInt("TakeDelay"), timerEventData);
    }

    // copied from BlockWorkStation
    public new void EventData_Event(TimerEventData timerData)
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