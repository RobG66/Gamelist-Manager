namespace Gamelist_Manager.Models;

public class GameReportItem
{
    public string Name { get; set; } = string.Empty;
    public string CloneOf { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NonPlayable { get; set; } = string.Empty;
    public string CHDRequired { get; set; } = string.Empty;
    public string NotInDat { get; set; } = string.Empty;
}
