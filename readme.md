# 7days To Die - Efficient Base Repair

This mod inspired by [ocbClaimAutoRepair](https://github.com/OCB7D2D/OcbClaimAutoRepair), add a new block allowing to efficiently repair your base in addition to dynamically showing some good to known repair statistics.

Almost fully customizable, this mod allows you to choose you own balancing settings. It means that you can configure it throught xml files, and choose to have an over-powered behavior, instantly repairing all your base without any required material. Or you can choose to listen the reasonable voice in your head and to choose a small repair rate, to slowly repair your base over the time.

## How to install ?

...

**The mod needs to be installed on both client and server side**

## How it works ?

It is based on a [BFS](https://en.wikipedia.org/wiki/Breadth-first_search) approach to find all blocks to repair. From the position where it is placed, it will recursively analyse his direct neighbors, then the neighbors of his neighbors, etc..

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

Instantly repairing 10,000,000 block will obviously have a cost...

## How to custom ?

Once the installation is done, go to [blocks.xml](./Config/blocks.xml) and look for the `Specific properties` section.

``` xml
<!-- Specific properties -->
<property name="MaxBfsIterations" value="1000"/> <!-- The max number of bfs iterations (more iterations will require more CPU ressources) -->
<property name="NeedsMaterials" value="true"/>	 <!-- Set this param to false, to fully disable the material requirements -->
<property name="LootSizeX" value="6"/>			 <!-- Number of columns of the loot container -->
<property name="LootSizeY" value="6"/>			 <!-- Number of rows of the loot container -->
<property name="RepairRate" value="1"/> 	 	 <!-- The number of block which can be repaired for one game tick (set to 0 to instant repair) -->
<property name="RefreshRate" value="5"/>	     <!-- The number of game ticks between two auto stats refresh (set to 0 to disable auto refresh) -->
```

## How to report a bug ?

send me you logfile + your options from `Mods/EfficientBaseRepair/Config/blocks.xml`
...

## How to build from sources ?

1. Configure the required environnement variables

    * `PATH_7D2D`: the path to the folder `path\to\7 Days To Die`

    * `7z.exe`: The executable of [7-Zip](https://www.7-zip.org/download.html) (needed to build the final zip file). It must be added to your env variable `Path`

2. Install [dotnet](https://dotnet.microsoft.com/en-us/download)

3. Build the dll `dotnet build` from the project root

4. (optional) Run [Release.cmd](./Scripts/release.cmd) to compile, then to enpack all the files in EfficientBaseRepair.zip and to load it in the Mod folder of the installation pointed by the env variable `PATH_7D2D`

## Knows issues

* Auto repairing might break the initial tensions of the neighbors blocks