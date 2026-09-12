using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;

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

        [OperationContract]
        void SendPrivateMessage(string fromUserId, string toUserId, string message);

        [OperationContract]
        List<string> GetPrivateMessages(string userId, string otherUserId);

        [OperationContract]
        void ShareFile(string channelName, string fromUserId, string fileName, byte[] fileData);

        [OperationContract]
        List<FileMetaInfo> GetSharedFiles(string channelName);

        [OperationContract]
        byte[] DownloadFile(string channelName, string fileId);
    }

    [DataContract]
    public class FileMetaInfo
    {
        [DataMember]
        public string FileId { get; set; }
        [DataMember]
        public string Filename { get; set; }
        [DataMember]
        public string Sender { get; set; }

        public override string ToString()
        {
            return $"{Filename} (from: {Sender})";
        }
    }
}