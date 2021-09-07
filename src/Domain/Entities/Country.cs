using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("Country", Schema = "blogic")]
    [Index(nameof(Code))]
    public class Country : BaseEntity
    {
        [Required]
        [MaxLength(60)]
        public string Name { get; set; }
        
        [Required]
        [MaxLength(2)]
        public string Code { get; set; }    

        public virtual ICollection<State> States { get; set; }
    }
}
