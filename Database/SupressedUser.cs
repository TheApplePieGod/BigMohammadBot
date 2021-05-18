using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class SupressedUser
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? TimeStarted { get; set; }
        public int MaxTimeSeconds { get; set; }
    }
}
