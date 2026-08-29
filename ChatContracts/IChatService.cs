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
        List<Channel> GetChannels();
    }
}