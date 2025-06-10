namespace CMAPI.DTO.Avaria;

public class RequestEditAvariaDTO
{
    public string TechinicianId { get; set; }
    public string TempoResolver { get; set; }
    public string Descricao { get; set; }
    public string StatusAvaria { get; set; }
    public string Location { get; set; }
}