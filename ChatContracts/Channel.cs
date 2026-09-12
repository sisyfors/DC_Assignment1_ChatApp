using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ChatContracts
{
    [DataContract]
    public class Channel
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Users { get; set; } = new List<string>();

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();

        [DataMember]
        public List<int> JoinIndexes { get; set; } = new List<int>();
    }
}