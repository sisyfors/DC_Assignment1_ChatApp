using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ChatContracts;
using System.Runtime.Serialization;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host;

            int safeLimit = 4 * 1024 * 1024;
            NetTcpBinding tcp = new NetTcpBinding()
            {
                MaxReceivedMessageSize = safeLimit,
                MaxBufferSize = safeLimit,
                TransferMode = TransferMode.Buffered
            };

            tcp.ReaderQuotas.MaxArrayLength = safeLimit;

            //This represents a tcp/ip binding in the Windows network stack
            //Bind server to the implementation of DataServer
            host = new ServiceHost(typeof(ChatService));
            /*Present the publicly accessible interface to the client. 0.0.0.0 tells .net to
            accept on any interface. :8100 means this will use port 8100. DataService is a name for the
            actual service, this can be any string.*/

            host.AddServiceEndpoint(typeof(IChatService), tcp, "net.tcp://0.0.0.0:8100/ChatService");

            host.AddServiceEndpoint(typeof(IDuplexChatService), tcp, "net.tcp://0.0.0.0:8100/DuplexChatService");
            //And open the host for business!


            host.Open();

            Console.WriteLine("Chat server started.");
            Console.WriteLine("Press ENTER to stop the server.");

            Console.ReadLine();

            host.Close();
        }
    }
}