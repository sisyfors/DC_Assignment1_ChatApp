using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ChatContracts
{
    [DataContract]
    public class PrivateChannel
    {
        [DataMember]
        public string Sender { get; set; }

        [DataMember]
        public string Recipient { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();
    }
}
