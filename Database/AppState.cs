using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigMohammadBot.Database
{
    public partial class AppState
    {
        [Key]
        public int Id { get; set; }
        public int? LastHelloUserId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastHelloMessage { get; set; }
        [Required]
        public bool? HelloDeleted { get; set; }
        public int HelloChannelId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? StatisticsPeriodStart { get; set; }
        public int HelloIteration { get; set; }
        public bool AutoCreateNewHello { get; set; }
        public bool? HelloTimerNotified { get; set; }
        public int KeeperUserId { get; set; }
        public int SuspendedUserId { get; set; }
        [Required]
        public string HelloTopic { get; set; }
        public int JoinMuteMinutes { get; set; }
    }
}
