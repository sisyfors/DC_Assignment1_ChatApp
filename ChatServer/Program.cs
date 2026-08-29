using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(ChatService));

            host.Open();

            Console.WriteLine("Chat server started.");
            Console.WriteLine("Press ENTER to stop the server.");

            Console.ReadLine();

            host.Close();
        }
    }
}