using System.Collections.Generic;
using HarmonyLib;
using Twitch;


[HarmonyPatch(typeof(GameManager))]
[HarmonyPatch("lootContainerOpened")]
public class GameManager_lootContainerOpened
{
    /*
    Patcher to display the EfficientBasRepair's custom UI and keeping the vanilla crates locking system.
    */
    public static bool Prefix(GameManager __instance, TileEntityLootContainer _te, LocalPlayerUI _playerUI, int _entityIdThatOpenedIt, ref World ___m_World)
    {
        if (!(_te is TileEntityEfficientBaseRepair))
            return true;

        FastTags<TagGroup.Global> containerTags = FastTags<TagGroup.Global>.none;
        if (_playerUI != null)
        {
            if (_te.entityId != -1)
            {
                Entity entity = ___m_World.GetEntity(_te.entityId);
                if (entity != null)
                {
                    if (entity.spawnById > 0 && entity.spawnById != _playerUI.entityPlayer.entityId && TwitchManager.Current.StealingCrateEvent != "")
                    {
                        // NOTE: I have no idea of what is doing this code, this why i keep it
                        GameEventManager.Current.HandleAction(TwitchManager.Current.StealingCrateEvent, _playerUI.entityPlayer, _playerUI.entityPlayer, twitchActivated: false);
                    }
                }
            }

            XUiC_EfficientBaseRepair.Open(_playerUI, _te as TileEntityEfficientBaseRepair);
        }

        // NOTE: i guess this code is responsible of crates locking, to prevent simultaeous access to multiple players ?
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            __instance.lootManager.LootContainerOpened(_te, _entityIdThatOpenedIt, containerTags);
            _te.bTouched = true;
            _te.SetModified();
        }

        return false;
    }
}


[HarmonyPatch(typeof(GameManager))]
[HarmonyPatch("TEUnlockServer")]
public class GameManager_TEUnlockServer
{
    public static bool Prefix(GameManager __instance, int _clrIdx, Vector3i _blockPos, int _lootEntityId, bool _allowContainerDestroy = true)
    {
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageTELock>().Setup(NetPackageTELock.TELockType.UnlockServer, _clrIdx, _blockPos, _lootEntityId, -1, null, _allowContainerDestroy));
            return false;
        }

        TileEntity tileEntity = null;
        if (_lootEntityId == -1)
        {
            tileEntity = __instance.m_World.GetTileEntity(_blockPos);
        }
        else
        {
            tileEntity = __instance.m_World.GetTileEntity(_lootEntityId);
            if (tileEntity == null)
            {
                foreach (KeyValuePair<ITileEntity, int> lockedTileEntity in __instance.lockedTileEntities)
                {
                    if (lockedTileEntity.Key.EntityId == _lootEntityId)
                    {
                        __instance.lockedTileEntities.Remove(lockedTileEntity.Key);
                        break;
                    }
                }
            }
        }

        if (tileEntity != null)
        {
            __instance.lockedTileEntities.Remove(tileEntity);

            if (tileEntity.GetTileEntityType() == (TileEntityType)191)
                return false;

            if (_allowContainerDestroy)
            {
                __instance.DestroyLootOnClose(tileEntity, _blockPos, _lootEntityId);
            }
        }

        return false;
    }
}

