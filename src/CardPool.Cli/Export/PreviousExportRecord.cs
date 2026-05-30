namespace CardPool.Cli.Export;

public sealed class PreviousExportRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsEligible { get; set; }
}
