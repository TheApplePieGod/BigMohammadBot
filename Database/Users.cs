using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigMohammadBot.Database
{
    public partial class Users
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(8)]
        public byte[] DiscordUserId { get; set; }
        [StringLength(200)]
        public string DiscordUserName { get; set; }
        public int ChainBreaks { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastActive { get; set; }
        public int AmountKeeper { get; set; }
    }
}
