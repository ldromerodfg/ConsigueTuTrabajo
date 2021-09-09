using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("CandidateStage", Schema = "blogic")]
    [Index(nameof(BreezyId))]
    public class CandidateStage : BaseEntity
    {
        public CandidateStage() { }

        public string BreezyId { get; set; }
        public string Name { get; set; }
    }
}
