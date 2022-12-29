using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ServiceApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string address = "net.tcp://localhost:5000/Chat";
            NetTcpBinding binding = new NetTcpBinding();

            ServiceHost host = new ServiceHost(typeof(Service));
            host.AddServiceEndpoint(typeof(IService), binding, address);

            host.Open();
            Console.WriteLine("Service started...");

            Console.ReadLine();
            host.Close();
        }
    }
}
