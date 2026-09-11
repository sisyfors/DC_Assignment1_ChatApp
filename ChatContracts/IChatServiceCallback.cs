using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ChatContracts
{
    public interface IChatServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string channelName, string userId, string message);
        [OperationContract(IsOneWay = true)]
        void ReceivePrivateMessage(string fromUserId, string message);
        [OperationContract(IsOneWay = true)]
        void ReceiveFile(string channelName, string fromUserId, string fileName, byte[] fileData);

        [OperationContract(IsOneWay = true)]
        void MemberlistChange(string channelName, string userId);

        [OperationContract(IsOneWay = true)]
        void ChannelListChange(string channelName);
    }
}
