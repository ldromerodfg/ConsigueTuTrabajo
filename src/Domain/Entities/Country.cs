using System.Collections.Generic;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Index(nameof(Short))]
    public class Country : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }    

        public virtual ICollection<State> States { get; set; }
    }
}
