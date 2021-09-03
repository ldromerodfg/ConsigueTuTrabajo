﻿using System;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Index(nameof(FileName))]
    public class Resume : BaseEntity
    {
        public Resume()
        {
            Created = DateTime.Now;
        }

        public string DirectoryPath { get; set; }
        public string FileName { get; set; }
        public string FileAttribute { get; set; }
        public decimal FileSize { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }

        public virtual Candidate Candidate { get; set; }
    }
}