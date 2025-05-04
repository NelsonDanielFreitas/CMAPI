using System.ComponentModel.DataAnnotations;

namespace CMAPI.DTO.Asset;

public class AddAssetDTO
{
    [Required]
    public string Name { get; set; }
        
    [Required]
    public string Description { get; set; }
        
    [Required]
    public string Location { get; set; }
        
    [Required]
    public DateTime InstalledDate { get; set; }
        
    [Required]
    public Guid AssetTypeId { get; set; }
        
    [Required]
    public Guid AssetStatusId { get; set; }
}