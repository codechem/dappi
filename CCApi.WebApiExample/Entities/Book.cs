using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CCApi.SourceGenerator.Attributes;

namespace CCApi.WebApiExample.Entities;

[CCController(CreateDto = true)]
public class Book
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Title { get; set; }
    public int YearPublished { get; set; }
    
    // Foreign key to reference the Author
    [Required]
    [ForeignKey("Author")]
    public Guid AuthorId { get; set; }

    // Navigation property: Each book has one author
    public Author? Author { get; set; }
}

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
    
    // Navigation property: One author can have many books
    // Required to have jsonIgnore and be nullable
    [JsonIgnore]
    public ICollection<Book>? Books { get; set; }
}