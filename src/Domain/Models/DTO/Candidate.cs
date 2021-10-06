using System;

namespace Web.Models
{
    public class CandidateResponse : CandidateRequest
    {
        public int Id { get; set; }
        public string BreezyId { get; set; }
        public string MetaId { get; set; }
        public string Headline { get; set; }
        public string Initial { get; set; }
        public string Origin { get; set; }
        public CandidateStageResponse Stage { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime Created { get; set; }
    }

    public class CandidateRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? PositionId { get; set; }
        public byte[] Resume { get; set; }
    }
}