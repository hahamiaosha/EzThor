namespace ThorFlasher.Core.Models;

public sealed class ThorCommandOptions
{
    public OperationCommandDefinition BinFlash { get; set; } = new();

    public OperationCommandDefinition CapUpdate { get; set; } = new();
}
