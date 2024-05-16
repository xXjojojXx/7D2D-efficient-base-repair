using System.Collections.Generic;

public class TileEntityEfficientBaseRepair : TileEntitySecureLootContainer {

    public bool is_on;
    public bool is_under_cooldown;

    public TileEntityEfficientBaseRepair(Chunk _chunk) : base(_chunk){
        is_on = false;
    }

    // copied from ocbClaimAutoRepair
    public override TileEntityType GetTileEntityType() => (TileEntityType)242;

    public Dictionary<string, int> FindAndRepairDamagedBlocks(World world) {
        return null;
    }
}