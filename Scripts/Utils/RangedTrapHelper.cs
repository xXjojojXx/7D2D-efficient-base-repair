using System.Collections.Generic;
using System.Linq;

public class RangedTrapHelper
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<RangedTrapHelper>();

    public static string GetAmmoItemName(TileEntityPoweredRangedTrap tileEntity)
    {
        var block = tileEntity.chunk.GetBlock(tileEntity.localChunkPos).Block;

        if (block is BlockLauncher blockLauncher)
        {
            return blockLauncher.AmmoItemName;
        }
        else if (block is BlockRanged blockRanged)
        {
            return blockRanged.AmmoItemName;
        }

        logger.Warning($"No ammo found for block '{block.blockName}'");

        return null;
    }

    public static List<ItemClass> GetAllowedAmmos(TileEntityPoweredRangedTrap tileEntity)
    {
        if (tileEntity.AmmoItem is ItemClass ammoItem)
            return new List<ItemClass>() { ammoItem };

        string itemName = GetAmmoItemName(tileEntity);

        if (itemName == null)
            return new List<ItemClass>();

        return itemName.Split(",")
            .Select(ammoName => ItemClass.GetItemClass(ammoName))
            .Where(itemClass => itemClass != null)
            .ToList();
    }

    public static ItemClass GetFirstAllowedAmmo(TileEntityPoweredRangedTrap tileEntity)
    {
        var allowedAmmos = GetAllowedAmmos(tileEntity);

        if (allowedAmmos.Count > 0)
        {
            return allowedAmmos[0];
        }

        return null;
    }
}