using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("Category", Schema = "blogic")]
    [Index(nameof(Code))]
    public class Category : BaseEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }

        public virtual ICollection<Position> Positions { get; set; }
    }
}