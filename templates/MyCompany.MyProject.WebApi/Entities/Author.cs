using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dappi.HeadlessCms.Models;
using Dappi.Core.Attributes;
using Dappi.HeadlessCms.Core.Attributes;

namespace MyCompany.MyProject.WebApi.Entities
{
    [CCController]
    public class Author
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public MediaInfo? Image { get; set; }

        [DappiRelation(DappiRelationKind.OneToMany, typeof(Book))]
        public ICollection<Book>? Books { get; set; }
    }
}