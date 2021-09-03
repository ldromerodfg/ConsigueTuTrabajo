using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("State", Schema = "blogic")]
    [Index(nameof(Short))]
    public class State : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }

        [ForeignKey("CountryId")]
        public int CountryId { get; set; }
        public virtual Country Country { get; set; }
        
        public virtual ICollection<City> Cities { get; set; }
    }
}
