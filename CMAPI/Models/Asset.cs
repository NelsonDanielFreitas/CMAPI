namespace CMAPI.Models;

public class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public Guid AssetTypeId { get; set; }
    public DateTime InstalledDate { get; set; }
    public Guid AssetStatusId { get; set; }  
    public AssetType AssetType { get; set; }
    public ICollection<Avaria> Avaria { get; set; }
    public AssetStatus Status { get; set; }  
}