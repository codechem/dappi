using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CCApi.SourceGenerator.Attributes;

namespace CCApi.WebApiExample.Entities;

[CCController]
public class Author
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [Range(0, 120)]
    public int Age { get; set; }

    public ICollection<Book>? Books { get; set; }
}