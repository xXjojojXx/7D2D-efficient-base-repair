# 7days To Die - Efficient Base Repair

This mod inspired by [ocbClaimAutoRepair](https://github.com/OCB7D2D/OcbClaimAutoRepair), add a new block allowing to efficiently repair your base in addition to dynamically showing some good to known repair statistics.

Almost fully customizable, this mod allows you to choose you own balancing settings. It means that you can configure it throught xml files, and choose to have an over-powered behavior, instantly repairing all your base without any required material. Or you can choose to listen the reasonable voice in your head and to choose a small repair rate, to slowly repair your base over the time, and stoping when it's out of ressources.

## How to install ?

...

**The mod needs to be installed on both client and server side**

## How to use ?

1. Place the EfficientBaseRepair block on the structure that you want to repair (see the next section for more details)

2. Open the EfficientBaseRepair block, and put into the required materials inside.

3. Press the `switch on` button from the user interface, or from the block command bar.

4. Here it is, now just wait for you base to be repaired !

## How damaged blocks are found ?

This mod uses a [BFS](https://en.wikipedia.org/wiki/Breadth-first_search) approach to find all blocks to repair. From the position where it is placed, it will recursively analyse his direct neighbors, then the neighbors of his neighbors, etc.. Until he found a terrain block, here he will stop to look for his neighbors (expect if this block has damages).

It means that it has to be placed directly on the structure that you want to be covered by auto-repair. You can also link two structures by connecting them with blocks that are not ignored in the BFS

Ignored blocks are determined as following:

``` C#
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

## And about performance ?

As this approcah may require a lot of ressources, the searching of blocks to repair cannot be done in real time. It's assumed that for big stuctures (more than 100,000 blocks) you'll experiment short screen freeze at the full structure analysing. It is the price to pay to have the benefit of this quick repair...

You can limit the covered search, by reducing the `MaxBfsIteration` parameter if you need a better performances compromise.

The full structure analysis will be done at three different times:

* A the block opening: When you open the UI of the block, it will automatically search for blocks to repair, in order to display fresh datas (Don't be worried if you have a small freeze delay at opening on big structures).

* Every `n` game ticks: You can parameter the number of game tick (one game tick ~ 2s) to perform an auto refresh when the block is activated. By default auto refresh are disabled.

* Manually: You dispose of a manual refresh button into the UI.

## How to custom ?

Once the installation is done, go to [blocks.xml](./Config/blocks.xml) and look for the `Specific properties` section.

``` xml
<!-- Specific properties -->
<property name="MaxBfsIterations" value="1000"/> <!-- The max number of bfs iterations (more iterations will require more CPU ressources) -->
<property name="NeedsMaterials" value="true"/>   <!-- Set this param to false, to fully disable the material requirements -->
<property name="LootSizeX" value="6"/>           <!-- Number of columns of the loot container -->
<property name="LootSizeY" value="6"/>           <!-- Number of rows of the loot container -->
<property name="RepairRate" value="100"/>        <!-- The amount of damage which can be repaired for one game tick (set to 0 for instant repairs) -->
<property name="RefreshRate" value="0"/>         <!-- The number of game ticks between two auto refresh (set to 0 to disable auto refresh) -->
<property name="PlayRepairSound" value="true"/>  <!-- Allow to enable / disable the hammer sound on the current block which is being repaired -->

```

## How to report a bug ?

send me you logfile + your options from `Mods/EfficientBaseRepair/Config/blocks.xml`
...

## FAQ

### I changed the balancing in blocks.xml but the changes were not applied after game reload.

Removing and replacing the EfficientBaseRepair block once the game was reloaded might be required to apply the configuration modifications.

### I have set the RepairRate to 100 but it repaired 120, what the ?

It's normal, the specified RepairRate is only a target. The real computed damages amount is based on the amount of material taken is the container.

For example, if one item of RessourceConcrete repairs 120 damages points but you specified a repairRate of 100, then the repaired amount will be 120 then no more block will be repaired in this tick, since the limit of 100 repairs per tick was reached.

### Can I use EfficientBaseRepair to auto upgrade my base ?

Not for now, but it's planned

### Can I use this mod on a muliplayer server ?

Yes, but be aware of the following points:

* EfficientBaseRepair does not check if the block you are trying to repair is inside you land claim or not
* Concurrency access to the loot container of EfficientBaseRepair might be broken, and cause items duplication

### Can I use this mod on UndeadLegacy or DarknessFalls ?

No test was made on such mods but as it implement a custom UI it might not be compatible.

Upvote [this github issue](link/to/issue) to manifest your interest about his feature.

### I was going for a trip in wasteland for hours, and when I came back my base was not repaired what happened ?

It's due to the core functionning of the game, you need to be near from the EfficientBlockRepair block to make it active.

It can be disturbing, because he's able to repair blocks much far away than the maximum distance that you can move away from him.

### I changed my mind about the block configuration, will I loose my save if I change it ?

No, all the parameters accessible from xml files can be changed whenever you want it. Just ensure to reload the game, and maybe to remove / re-place the block in game.

## Knows issues (aka to be fixed)

* Spike blocks at stage Dmg1 or Dmg2 with block.damage = 0 are upgraded for free at stage Dmg0
* Auto repairing might break the initial tensions of the neighbors blocks