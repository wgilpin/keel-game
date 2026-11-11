namespace Keel.Sim.Grid;

public static class BlockCosts
{
    public const int ArmorBlockCost = 1;
    public const int SolarPanelCost = 2;
    public const int CargoContainerCost = 2;

    public static int GetSteelBlockCost(BlockType type) => type switch
    {
        BlockType.ArmorBlock => ArmorBlockCost,
        BlockType.SolarPanel => SolarPanelCost,
        BlockType.CargoContainer => CargoContainerCost,
        _ => 0,
    };

    public static BlockType GetBlockTypeForSlot(int slotIndex) => slotIndex switch
    {
        2 => BlockType.ArmorBlock,
        4 => BlockType.SolarPanel,
        5 => BlockType.CargoContainer,
        _ => BlockType.None,
    };
}
