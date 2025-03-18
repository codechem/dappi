using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System;
using CCApi.SourceGenerator.Attributes;
namespace CCApi.WebApiExample.Entities;
[CCController]
public class testt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string t { get; set; }

    public string asd { get; set; }
}
