using UnityEngine;
using System.Collections.Generic;
using System;
using static Block;

public class TileEntityEfficientBaseRepair : TileEntitySecureLootContainer {

    public bool is_on;

    public TileEntityEfficientBaseRepair(Chunk _chunk) : base(_chunk){
        is_on = false;
    }

    // copied from ocbClaimAutoRepair
    public override TileEntityType GetTileEntityType() => (TileEntityType)242;

    public Dictionary<string, int> FindAndRepairDamagedBlocks(World world) {
        return null;
    }

}