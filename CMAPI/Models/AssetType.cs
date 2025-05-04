namespace CMAPI.Models;

public class AssetType
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public ICollection<Asset> Assets { get; set; }
}