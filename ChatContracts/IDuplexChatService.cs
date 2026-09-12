using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatContracts
{
    [ServiceContract(CallbackContract = typeof(IChatServiceCallback))]
    public interface IDuplexChatService : IChatService
    {
        [OperationContract]
        void RegisterCallback(string userId);
    
    }
}
