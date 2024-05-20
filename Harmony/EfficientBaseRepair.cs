using System.Reflection;
using HarmonyLib;

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
                if (type == (TileEntityType)243) {
                    __result = new TileEntityEfficientBaseRepair(_chunk);
                    return false;
                }
                return true;
            }
        }
    }
}