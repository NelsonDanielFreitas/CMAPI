namespace CMAPI.DTO.Asset;

public class AssetDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public DateTime InstalledDate { get; set; }
    public Guid AssetTypeId { get; set; }
    public string AssetTypeName { get; set; }
    public Guid AssetStatusId { get; set; }
    public string AssetStatusName { get; set; }
}