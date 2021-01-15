using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigMohammadBot.Database
{
    public partial class Channels
    {
        [Key]
        public int Id { get; set; }
        public short Type { get; set; }
        [Required]
        [MaxLength(8)]
        public byte[] DiscordChannelId { get; set; }
        [Required]
        [StringLength(200)]
        public string DiscordChannelName { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime LastActive { get; set; }
        public bool Deleted { get; set; }
    }
}
