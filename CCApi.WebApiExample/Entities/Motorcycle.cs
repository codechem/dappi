using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CCApi.SourceGenerator.Attributes;
namespace CCApi.WebApiExample.Entities;

[CCController]
[DappiAuthorize(authenticated: true, roles: ["Admin"], methods: ["POST"])]
[DappiAuthorize(authenticated: true, roles: ["User"], methods: ["GET"])]
public class Motorcycle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }

    public int Year { get; set; }
}
