﻿using System;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Index(nameof(BreezyId))]
    public class Company : BaseEntity
    {
        public string BreezyId { get; set; }
        public string Name { get; set; }
        public string FriendlyId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public int MemberCount { get; set; }
        public string Initial { get; set; }
    }
}
