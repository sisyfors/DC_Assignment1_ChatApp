using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ChatContracts
{
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract]
        bool SignIn(string userId, out string reason);

        [OperationContract]
        bool SignOut(string userId, out string reason);

        [OperationContract]
        bool CreateChannel(string channelName);

        [OperationContract]
        void JoinChannel(string userId, string channelName);

        [OperationContract]
        bool LeaveChannel(string userId, string channelName);

        [OperationContract]
        void SendMessage(string channelName, string userId, string message);

        [OperationContract]
        List<string> GetUsers(string channelName, string userId);

        [OperationContract]
        List<Channel> GetChannels();

        [OperationContract]
        List<string> GetMessages(string channelName, string userId);
    }
}