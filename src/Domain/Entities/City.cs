using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Index(nameof(Short))]
    public class City : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }

        public virtual ICollection<Position> Positions { get; set; }

        [ForeignKey("StateId")]
        public int StateId { get; set; }
        public virtual State State { get; set; }
    }
}
