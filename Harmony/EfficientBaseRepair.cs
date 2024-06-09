using System.Reflection;
using HarmonyLib;
using Twitch;

namespace Harmony
{
    public class EfficientBaseRepair : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new HarmonyLib.Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(TileEntity))]
        [HarmonyPatch("Instantiate")]
        public class TileEntity_Instantiate
        {
            public static bool Prefix(TileEntityType type, Chunk _chunk, ref TileEntity __result)
            {
                if (type == (TileEntityType)191)
                {
                    __result = new TileEntityEfficientBaseRepair(_chunk);
                    return false;
                }
                return true;
            }
        }

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

                FastTags containerTags = FastTags.none;
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
    }
}