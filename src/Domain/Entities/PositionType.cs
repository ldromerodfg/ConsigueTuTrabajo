using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("PositionType", Schema = "blogic")]
    [Index(nameof(Short))]
    public class PositionType : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }

        public virtual ICollection<Position> Positions { get; set; }
    }
}
