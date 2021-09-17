using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Table("Setting", Schema = "setting")]
    [Index(nameof(BreezyToken))]
    public class Setting: BaseEntity
    {
        public Setting() { }

        public string BreezyToken {get; set;}
    }
}