using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
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

            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            

            ServiceHost host = new ServiceHost(typeof(Service));

            host.AddServiceEndpoint(typeof(IService), binding, address);

            host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
            ///If CA doesn't have a CRL associated, WCF blocks every client because it cannot be validated
            host.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            const string sreviceCertCN = "serviceApp";

            /// Get the private (.pfx) certificate for Replicator Service from LocalMachine\My (Personal)
            host.Credentials.ServiceCertificate.Certificate = CertificateManager.GetCertificateFromStorage(
                StoreName.My, StoreLocation.LocalMachine, sreviceCertCN);

            host.Open();
            Console.WriteLine("Service started...");

            Console.ReadLine();
            host.Close();
        }
    }
}
