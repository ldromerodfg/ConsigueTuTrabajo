using System.Collections.Generic;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Index(nameof(Short))]
    public class Category : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }

        public virtual ICollection<Position> Positions { get; set; }
    }
}