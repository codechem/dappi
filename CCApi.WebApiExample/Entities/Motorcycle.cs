using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System;
using CCApi.SourceGenerator.Attributes;
namespace CCApi.WebApiExample.Entities;
[CCController]
public class Motorcycle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }

    public int Year { get; set; }
}
