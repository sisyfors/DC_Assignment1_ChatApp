using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatContracts
{
    public class Channel
    {
        public string Name { get; set; }

        public List<string> Users { get; set; } = new List<string>();

        public List<string> Messages { get; set; } = new List<string>();

        public List<int> JoinIndexes { get; set; } = new List<int>();
    }
}