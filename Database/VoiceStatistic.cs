using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    public partial class VoiceStatistic
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TimeInChannel { get; set; }
        public int ChannelId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastInChannel { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? TimePeriod { get; set; }
    }
}
