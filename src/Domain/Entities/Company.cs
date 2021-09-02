using System;
using System.Collections.Generic;
using Domain.Common;

namespace Domain.Entities
{
    public class Company : BaseEntity
    {
        public string BreezyId { get; set; }
        public string Name { get; set; }
        public string FriendlyId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public int MemberCount { get; set; }
        public string Initial { get; set; }
    }
}
