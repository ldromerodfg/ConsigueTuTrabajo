using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("City", Schema = "blogic")]
    [Index(nameof(Name))]
    public class City : BaseEntity
    {
        [Required]
        [MaxLength(60)]
        public string Name { get; set; }

        [ForeignKey("StateId")]
        public int StateId { get; set; }
        public virtual State State { get; set; }

        public virtual ICollection<Position> Positions { get; set; }
    }
}
