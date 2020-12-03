using System;
using System.Collections.Generic;
using System.Text;

namespace BigMohammadBot.Database.FunctionModels
{
    public class UserActivity
    {
        public string UserName { get; set; }
        public int TotalMessages { get; set; }
        public int TotalSecondsInVoice { get; set; }
    }
}
