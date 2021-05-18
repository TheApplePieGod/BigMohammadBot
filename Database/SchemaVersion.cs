using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class SchemaVersion
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public string ScriptName { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Applied { get; set; }
    }
}
