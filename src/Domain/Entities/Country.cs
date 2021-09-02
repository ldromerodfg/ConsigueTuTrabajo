using System.Collections.Generic;
using Domain.Common;

namespace Domain.Entities
{
    public class Country : BaseEntity
    {
        public string Name { get; set; }
        public string Short { get; set; }    

        public virtual ICollection<State> States { get; set; }
    }
}
