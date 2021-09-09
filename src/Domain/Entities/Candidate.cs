using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("Candidate", Schema = "blogic")]
    [Index(nameof(BreezyId))]
    [Index(nameof(Email))]
    public class Candidate : BaseEntity
    {
        public Candidate() { }

        public string BreezyId { get; set; }
        public string MetaId { get; set; }
        public string Email { get; set; }
        public string Headline { get; set; }
        public string Initial { get; set; }
        public string Name { get; set; }
        public string Origin { get; set; }
        public string PhoneNumber { get; set; }
        // TODO: ProfilePhotoURL

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }

        [ForeignKey("PositionId")]
        public int PositionId { get; set; }
        public virtual Position Position { get; set; }

        [ForeignKey("ResumeId")]
        public int? ResumeId { get; set; }
        public virtual Resume Resume { get; set; }

        [ForeignKey("CandidateStageId")]
        public int CandidateStageId { get; set; }
        public virtual CandidateStage Stage { get; set; }
    }
}