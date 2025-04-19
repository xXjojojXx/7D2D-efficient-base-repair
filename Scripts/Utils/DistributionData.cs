using System;
using System.Collections.Generic;

public class DistributionData
{
    public TileEntity tileEntity;

    public ItemClass itemClass;

    public int itemCount;

    public ItemStack[] ToItemStacks()
    {
        if (itemCount <= 0)
        {
            return Array.Empty<ItemStack>();
        }

        var itemStacks = new List<ItemStack>();
        var maxStackSize = itemClass.Stacknumber.Value;
        var remainingItems = itemCount;
        var itemValue = new ItemValue(itemClass.Id);

        while (remainingItems > 0)
        {
            var itemCount = Utils.FastMin(remainingItems, maxStackSize);
            remainingItems -= itemCount;

            itemStacks.Add(new ItemStack(itemValue, itemCount));
        }

        return itemStacks.ToArray();
    }
}
