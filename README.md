# 7 Days to Die - A21 - Efficient Base Repair

This mod, inspired by [ocbClaimAutoRepair](https://github.com/OCB7D2D/OcbClaimAutoRepair), adds a new block that allows you to efficiently repair your base while dynamically displaying useful repair statistics.

Almost fully customizable, this mod allows you to choose your own balancing settings. You can configure it through XML files to have an overpowered behavior, instantly repairing your entire base without any required materials. Or, you can listen to the reasonable voice in your head and choose a small repair rate to slowly repair your base over time, stopping when it's out of resources.

This block is unlocked at level 60 of repair tool skills and can be crafted in a workbench with forged steel, electric parts, and a car battery.

Find more details about changes of this project in the dedicated [changelog file](./CHANGELOG.md)

## How to Install Manually

1. Download the [zip file](https://www.nexusmods.com/7daystodie/mods/4861?tab=files) of the mod.
2. Extract the entire content of the zip file to `C:\...\Steam\steamapps\common\7 Days To Die\Mods\`.

#### NOTES
* **This mod is not EAC compatible.**
* **Installation on both client and server side is required.**
* **Consider doing a backup of your current save before installing the mod.**
* **Before uninstalling the mod, ensure to remove all EfficientBaseRepair blocks from the world.**

## How to Use

1. Place the EfficientBaseRepair block on the structure that you want to repair.
2. Open the EfficientBaseRepair block and put the required materials inside.
3. (optional) press the `Upgrade On` button to enable the blocks upgrade
4. Press the `Turn on` button from the user interface or from the block command bar.
5. That's it! Now just wait for your base to be repaired!

## How Are Damaged Blocks Found?

This mod uses a [BFS](https://en.wikipedia.org/wiki/Breadth-first_search) approach to find all blocks to repair. From the position where it is placed, it will recursively analyze its direct neighbors, then the neighbors of its neighbors, etc., until it finds water, air, or a terrain block. It will stop looking for neighbors unless this block has damage.

This means that it has to be placed directly on the structure that you want to be covered by auto-repair, and this structure must be continuous to be fully covered.

To be clear and concise: **The EfficientBaseRepair block must be able to find a path to the target block you want to be repaired.**

Ignored blocks are determined as follows:

```C#
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
```

## What About Performance?

As this approach may require a lot of resources, the search for blocks to repair cannot be done in real time. It's assumed that for big structures (more than 100,000 blocks), you might experience some short framerate drops during the full structure analysis. This is the price to pay to have the benefit of quick repairs.

You can limit the search coverage by reducing the `MaxBfsIteration` parameter if you need a better performance compromise.

The full structure analysis will be done at different moments:

* At the block opening: When you open the UI of the block, it will automatically search for blocks to repair to display fresh data. (Don't be worried if you experience a small freeze delay when opening on big structures.)
* At the block activation from the block commands (radial menu).
* Every `n` game ticks: You can set the number of game ticks (one game tick ≈ 2s) to perform an auto-refresh when the block is activated. By default, auto-refresh is disabled.
* Manually: You have a manual refresh button in the UI.

## How to Customize

Once the installation is done, go to [blocks.xml](./Config/blocks.xml) and look for the `Specific properties` section.

```xml
<!-- Specific properties -->
<property name="MaxBfsIterations" value="1000"/>            <!-- The max number of bfs iterations (more iterations will require more CPU ressources) -->
<property name="NeedsMaterialsForRepair" value="true"/>     <!-- Set this param to false to repair even if you miss the repair materials -->
<property name="NeedsMaterialsForUpgrade" value="true"/>    <!-- Set this param to false to upgrade even if you miss the upgrade materials -->
<property name="LootSizeX" value="6"/>                      <!-- Number of columns of the loot container -->
<property name="LootSizeY" value="5"/>                      <!-- Number of rows of the loot container -->
<property name="RefreshRate" value="0"/>                    <!-- The number of game ticks between two auto refresh (set to 0 to disable auto refresh) -->
<property name="RepairRate" value="100"/>                   <!-- The amount of damage which can be repaired for one game tick (set to 0 for instant repairs) -->
<property name="UpgradeRate" value="1"/>                    <!-- The number of blocks which can be upgraded during one game tick (set to 0 to for instant upgrades) -->
<property name="PlayRepairSound" value="true"/>             <!-- Set to false to disable the hammer sound on the current block which is being repaired -->
<property name="ActiveDuringBloodMoon" value="false"/>      <!-- Set to false to disable the auto repair during the blood moon -->
<property name="AutoTurnOff" value="true"/>                 <!-- Auto turn off if no more blocks can be repaired -->
<property name="KeepPaintAfterUpgrade" value="true"/>       <!-- If enabled, the upgraded block will keep his original paint, else his texture will be set to the upgrade block texture -->
```

Once you have set up your own balancing options, restart the game or the dedicated server.
