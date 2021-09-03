using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("Company", Schema = "blogic")]
    [Index(nameof(BreezyId))]
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
