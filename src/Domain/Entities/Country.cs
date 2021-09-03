using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("Country", Schema = "blogic")]
    [Index(nameof(Short))]
    public class Country : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }    

        public virtual ICollection<State> States { get; set; }
    }
}
