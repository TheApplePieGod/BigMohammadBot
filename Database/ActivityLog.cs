using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    [Table("ActivityLog")]
    public partial class ActivityLog
    {
        [Key]
        public int Id { get; set; }
        public int TypeId { get; set; }
        [Required]
        [StringLength(100)]
        public string Information { get; set; }
        public int CalledByUserId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime CallTime { get; set; }
        [StringLength(200)]
        public string ResultText { get; set; }
        public bool Success { get; set; }
    }
}
