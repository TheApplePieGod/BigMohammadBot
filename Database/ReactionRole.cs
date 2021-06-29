using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class ReactionRole
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(8)]
        public byte[] MessageId { get; set; }
        public bool Deleted { get; set; }
    }
}
