using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class ReactionRoleEmote
    {
        [Key]
        public int Id { get; set; }
        public int ReactionRoleId { get; set; }
        [Required]
        public string Emote { get; set; }
        [Required]
        [MaxLength(8)]
        public byte[] RoleId { get; set; }
    }
}
