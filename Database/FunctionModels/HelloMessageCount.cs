using System;
using System.Collections.Generic;
using System.Text;

namespace BigMohammadBot.Database.FunctionModels
{
    public class HelloMessageCount
    {
        public int UserId { get; set; }
        public int Iteration { get; set; }
        public int NumMessages { get; set; }
    }
}
