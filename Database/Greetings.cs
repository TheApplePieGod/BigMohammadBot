using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigMohammadBot.Database
{
    public partial class Greetings
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(200)]
        public string Greeting { get; set; }
        public int Iteration { get; set; }
    }
}
