public class Config
{
    private static readonly ModConfig config = new ModConfig("EfficientBaseRepair");

    public static int maxBfsIterations = config.GetInt("maxBfsIterations");

    public static bool needMaterialsForRepair = config.GetBool("needMaterialsForRepair");

    public static bool needMaterialsForUpgrade = config.GetBool("needMaterialsForUpgrade");

    public static bool activeDuringBloodMoon = config.GetBool("activeDuringBloodMoon");

    public static int repairRate = config.GetInt("repairRate");

    public static int refreshRate = config.GetInt("refreshRate");

    public static bool playRepairSound = config.GetBool("playRepairSound");

    public static bool autoTurnOff = config.GetBool("autoTurnOff");

    public static int upgradeRate = config.GetInt("upgradeRate");

    public static bool keepPaintAfterUpgrade = config.GetBool("keepPaintAfterUpgrade");

    public static Vector2i lootSize = config.GetVector2i("lootSize");

    public const TileEntityType tileEntityType = (TileEntityType)191;
}