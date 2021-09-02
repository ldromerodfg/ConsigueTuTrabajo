using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;

namespace Domain.Entities
{
    public class Position : BaseEntity
    {
        public string Name { get; set; }
        public string BreezyId { get; set; }
        public string State { get; set; }
        public string Description { get; set; }
        public string Education { get; set; }
        public string Department { get; set; }
        public string RequisitionId { get; set; }
        public string QuestionaireId { get; set; }
        public string PipelineId { get; set; }
        public string CandidateType { get; set; }
        public string Tags { get; set; }
        public string OrgType { get; set; }

        public string CreatorId { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }

        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        [ForeignKey("PositionTypeId")]
        public int PositionTypeId { get; set; }
        public virtual PositionType Type { get; set; }

        [ForeignKey("CategoryId")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        [ForeignKey("CityId")]
        public int CityId { get; set; }
        public virtual City City { get; set; }
    }
}
