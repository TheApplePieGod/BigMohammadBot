using System;
using System.Collections.Generic;
using System.Text;

namespace BigMohammadBot.Database.FunctionModels
{
    public class LastLogs
    {
        public string TypeName { get; set; }
        public string Information { get; set; }
        public string CalledByUserName { get; set; }
        public DateTime CallTime { get; set; }
        public string ResultText { get; set; }
        public bool Success { get; set; }
    }
}
