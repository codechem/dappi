using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CCApi.SourceGenerator.Attributes;
namespace MyCompany.MyProject.WebApi.Entities;
[CCController]
public class TestName
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public  string? FieldsAHAHA { get; set; }

    public  string? asdffdsaasdf { get; set; }

    public  string? fff { get; set; }

    public  string? Fasdf { get; set; }
}
