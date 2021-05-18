using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BigMohammadBot.Database
{
    [Table("ChannelBlacklist")]
    public partial class ChannelBlacklist
    {
        [Key]
        public int Id { get; set; }
        public int ChannelId { get; set; }
    }
}
