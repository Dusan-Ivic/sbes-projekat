using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string address = "net.tcp://localhost:5001/Monitoring";
            NetTcpBinding binding = new NetTcpBinding();

            ServiceHost host = new ServiceHost(typeof(Monitoring));

            host.AddServiceEndpoint(typeof(IMonitoring), binding, address);

            host.Open();
            Console.WriteLine("Monitoring started...");

            Console.ReadLine();
            host.Close();
        }
    }
}
