using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigMohammadBot.Database
{
    public partial class MessageStatistics
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MessagesSent { get; set; }
        public int ChannelId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastSent { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? TimePeriod { get; set; }
    }
}
