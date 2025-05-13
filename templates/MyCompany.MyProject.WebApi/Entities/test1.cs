using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CCApi.SourceGenerator.Attributes;
using CCApi.Extensions.DependencyInjection.Models;
namespace MyCompany.MyProject.WebApi.Entities;
[CCController]
public class test1
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public  string? asdf { get; set; }

    public  ICollection<testCollection?> testCollections { get; set; } = new List<testCollection?>();
}
