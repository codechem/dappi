using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System;
using CCApi.SourceGenerator.Attributes;
namespace CCApi.WebApiExample.Entities;
[CCController]
public class test
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string asdf { get; set; }
}
