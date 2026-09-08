using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatContracts
{
    public class PrivateChannel
    {
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }
}
