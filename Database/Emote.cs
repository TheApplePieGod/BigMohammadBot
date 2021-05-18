using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class Emote
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Link { get; set; }
        [Required]
        [StringLength(25)]
        public string Name { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastUsed { get; set; }
    }
}
