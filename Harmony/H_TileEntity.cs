using HarmonyLib;


[HarmonyPatch(typeof(TileEntity))]
[HarmonyPatch("Instantiate")]
public class TileEntity_Instantiate
{
    public static bool Prefix(TileEntityType type, Chunk _chunk, ref TileEntity __result)
    {
        if (type == Config.tileEntityType)
        {
            __result = new TileEntityEfficientBaseRepair(_chunk);
            return false;
        }
        return true;
    }
}

