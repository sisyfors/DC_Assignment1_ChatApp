using ChatContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DuplexClient
{
    public class CallbackHandler : IChatServiceCallback
    {
        public void ReceiveMessage(string channelName, string userId, string message)
        {
            // Handle received message
        }

        public void ReceivePrivateMessage(string fromUserId, string message)
        {
            // Handle received private message
        }

        public void ReceiveFile(string channelName, string fromUserId, string fileName, byte[] fileData)
        {
            // Handle received file
        }

        public void MemberlistChange(string channelName, string userId)
        {
            // Handle member list change
        }

        public void ChannelListChange(string channelName)
        {
            // Handle channel list change
        }
    }
}
