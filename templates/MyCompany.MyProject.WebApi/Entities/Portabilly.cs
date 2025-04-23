using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CCApi.SourceGenerator.Attributes;
namespace MyCompany.MyProject.WebApi.Entities;
[CCController]
public class Portabilly
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public  string? Torta { get; set; }
}
