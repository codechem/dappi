using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dappi.HeadlessCms.Models;
using Dappi.Core.Attributes;
using Dappi.HeadlessCms.Core.Attributes;

namespace MyCompany.MyProject.WebApi.Entities
{
    [CCController]
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int Rating { get; set; }
        public DateOnly ReleaseDate { get; set; }

        [DappiRelation(DappiRelationKind.ManyToOne, typeof(Author))]
        public Author? Author { get; set; }

        [DappiRelation(DappiRelationKind.ManyToOne, typeof(Author))]
        public Guid? AuthorId { get; set; }
    }
}