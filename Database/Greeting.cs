using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class Greeting
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [Column("Greeting")]
        [StringLength(200)]
        public string Greeting1 { get; set; }
        public int Iteration { get; set; }
    }
}
