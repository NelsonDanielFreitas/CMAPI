using Microsoft.AspNetCore.Mvc;

namespace CMAPI.DTO.Avaria;

public class CreateAvariaRequestDto
{
    //[FromForm(Name = "IdUrgencia")]
    public string IdUrgencia { get; set; }

    //[FromForm(Name = "IsProdutoInstitucional")]
    public string IsProdutoInstitucional { get; set; } 

    //[FromForm(Name = "AssetId")]
    public string AssetId { get; set; } 

    //[FromForm(Name = "Descricao")]
    public string Descricao { get; set; } 

    /// <summary>
    /// This matches the MultipartBody.Part name "photo"
    /// </summary>
    //[FromForm(Name = "photo")]
    public string Photo { get; set; }

    //[FromForm(Name = "Localizacao")]
    public string Localizacao { get; set; } 
}