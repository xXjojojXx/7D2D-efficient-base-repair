# 7 Days to Die - A21 - Efficient Base Repair

This mod, inspired by [ocbClaimAutoRepair](https://github.com/OCB7D2D/OcbClaimAutoRepair), adds a new block that allows you to efficiently repair your base while dynamically displaying useful repair statistics.

Almost fully customizable, this mod allows you to choose your own balancing settings. You can configure it through XML files to have an overpowered behavior, instantly repairing your entire base without any required materials. Or, you can listen to the reasonable voice in your head and choose a small repair rate to slowly repair your base over time, stopping when it's out of resources.

This block is unlocked at level 60 of repair tool skills and can be crafted in a workbench with forged steel, electric parts, and a car battery.

Find more details about changes of this project in the dedicated [changelog file](./CHANGELOG)

## How to Install Manually

1. Download the [zip file](https://www.nexusmods.com/7daystodie/mods/4861?tab=files) of the mod.
2. Extract the entire content of the zip file to `...\steamapps\common\7 Days To Die\Mods\`.

#### NOTES
* **This mod is not EAC compatible.**
* **Installation on both client and server side is required.**
* **Consider doing a backup of your current save before installing the mod.**
* **Before uninstalling the mod, ensure to remove all EfficientBaseRepair blocks from the world.**

## How to Use

1. Place the EfficientBaseRepair block on the structure that you want to repair.
2. Open the EfficientBaseRepair block and put the required materials inside.
3. Press the `Turn on` button from the user interface or from the block command bar.
4. That's it! Now just wait for your base to be repaired!

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
<property name="MaxBfsIterations" value="1000"/>       <!-- The max number of BFS iterations (more iterations will require more CPU resources) -->
<property name="NeedsMaterials" value="true"/>         <!-- Set this parameter to false to fully disable material requirements -->
<property name="LootSizeX" value="6"/>                 <!-- Number of columns of the loot container -->
<property name="LootSizeY" value="6"/>                 <!-- Number of rows of the loot container -->
<property name="RepairRate" value="100"/>              <!-- The amount of damage that can be repaired per game tick (set to 0 for instant repairs) -->
<property name="RefreshRate" value="0"/>               <!-- The number of game ticks between two auto-refreshes (set to 0 to disable auto-refresh) -->
<property name="PlayRepairSound" value="true"/>        <!-- Set to false to disable the hammer sound on the current block being repaired -->
<property name="ActiveDuringBloodMoon" value="false"/> <!-- Set to false to disable auto repair during the blood moon -->
<property name="AutoTurnOff" value="false"/>           <!-- Auto turn off the block if no more blocks can be repaired -->
```

Once you have set up your own balancing options, restart the game or the dedicated server.

## FAQ

### How 'Efficient' is it?

It will be as efficient as you want it to be!

With the default settings, you can expect to repair about 100 hit points per minute, but you can configure it to instantly repair over 100,000 blocks! Obviously, there will be a (minimal) cost in terms of game performance. For small structures, you may not notice the calculation time, but for structures over 100,000 blocks, be mentally prepared for a wait of between 0.5 and 2 seconds.

So yes, it is efficient! (Much more so than manually searching for damaged blocks.)

### I have set the RepairRate to 100 but it repaired 120. Why?

This is normal. The specified RepairRate is only a target. The actual amount of damage repaired is based on the amount of material taken from the container.

For example, if one unit of ResourceConcrete repairs 120 damage points but you specified a RepairRate of 100, then the repaired amount will be 120, and no more blocks will be repaired in this tick since the limit of 100 repairs per tick was reached.

### Can I use this mod on a multiplayer server?

Yes, but be aware of the following points:

* This mod is not EAC compatible.
* EfficientBaseRepair does not check if the block you are trying to repair is inside your land claim or not. It can repair blocks much farther away than the maximum distance covered by your land claim block.
* Block finding is done on the server side and might be resource-intensive for big structures (so tell your friends to avoid spamming the refresh button at the same time).

### Can I use this mod on Undead Legacy or Darkness Falls?

No tests have been conducted on such mods, so compatibility is not guaranteed.

Upvote these GitHub issues to show your interest in [Undead Legacy](https://github.com/VisualDev-FR/7D2D-efficient-base-repair/issues/1) or [DarknessFalls](https://github.com/VisualDev-FR/7D2D-efficient-base-repair/issues/2) compatibility.

You can also [create another GitHub issue](https://github.com/VisualDev-FR/7D2D-efficient-base-repair/issues/new) to request compatibility with another popular mod.

### I was on a trip in the wasteland for hours, and when I came back, my base was not repaired. What happened?

This is due to the core functionality of the game. You need to be near the EfficientBaseRepair block to make it active.

It can be confusing because the block can repair structures much farther away than the maximum distance you can move away from it.

### I changed my mind about the block configuration. Will I lose my save if I change it?

No, all the parameters accessible from the XML files can be changed whenever you want. Just ensure to reload the game and possibly remove/re-place the block in-game.

## Known Issues (to be fixed)

* Spike blocks at stage Dmg1 or Dmg2 with block.damage = 0 are upgraded for free to stage Dmg0.
* Auto repairing might break the initial tensions of the neighboring blocks.
