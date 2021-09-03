using System;

namespace Web.Models 
{
    public class PositionResponse
    {
        public int Id { get; set; }
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
        public DateTime? Updated { get; set; }
        public DateTime Created { get; set; }
    }
}