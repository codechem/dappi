using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CCApi.SourceGenerator.Attributes;
using CCApi.Extensions.DependencyInjection.Models;
namespace MyCompany.MyProject.WebApi.Entities;
[CCController]
public class TestCollection
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public  string? TestField { get; set; }
}
