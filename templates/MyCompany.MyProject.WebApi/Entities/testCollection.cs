using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CCApi.SourceGenerator.Attributes;
using CCApi.Extensions.DependencyInjection.Models;
namespace MyCompany.MyProject.WebApi.Entities;
[CCController]
public class testCollection
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public  MediaInfo? TestImage { get; set; }

    public  string TestString { get; set; }

    public  string? sdf { get; set; }

    public  ICollection<test1?> test1s { get; set; } = new List<test1?>();

    public  string? name { get; set; }
}
