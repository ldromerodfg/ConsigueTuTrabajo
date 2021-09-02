using System.Collections.Generic;
using Domain.Common;

namespace Domain.Entities
{
    public class PositionType : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }

        public virtual ICollection<Position> Positions { get; set; }
    }
}
