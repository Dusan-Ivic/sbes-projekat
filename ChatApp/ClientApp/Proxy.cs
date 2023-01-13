using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    class ServiceProxy : ChannelFactory<IService>, IService, IDisposable
    {
        private IService factory;

        public ServiceProxy(NetTcpBinding binding, EndpointAddress address)
            : base(binding, address)
        {
            factory = this.CreateChannel();
        }

        public void Dispose()
        {
            if (factory != null)
            {
                factory = null;
            }

            this.Close();
        }

        public User Connect(string username)
        {
            try
            {
                return factory.Connect(username);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return null;
            }
        }

        public List<User> GetUsers()
        {
            try
            {
                return factory.GetUsers();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return new List<User>();
            }
        }

        public void Disconnect(User user)
        {
            try
            {
                factory.Disconnect(user);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }
    }
    
    class MonitoringProxy : ChannelFactory<IMonitoring>, IMonitoring, IDisposable
    {
        private IMonitoring factory;

        public MonitoringProxy(NetTcpBinding binding, EndpointAddress address)
            : base(binding, address)
        {
            factory = this.CreateChannel();
        }

        public void Dispose()
        {
            if (factory != null)
            {
                factory = null;
            }

            this.Close();
        }
        
        public void Log(Message message)
        {
            try
            {
                factory.Log(message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }
    }

    class ChatProxy : ChannelFactory<IChat>, IChat, IDisposable
    {
        private IChat factory;

        public ChatProxy(NetTcpBinding binding, EndpointAddress address, string username)
            : base(binding, address)
        {
            this.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.Custom;
            this.Credentials.ServiceCertificate.Authentication.CustomCertificateValidator = new CertValidator();
            this.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            // Zakomentarisati ovog
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Path.Combine(Directory.GetParent(workingDirectory).FullName, @"Common\Certificates");
            string certificatePath = Path.Combine(projectDirectory, $"{username}.pfx");
            
            this.Credentials.ClientCertificate.Certificate = CertificateManager.GetCertificateFromFile(certificatePath);

            // Otkomentarisati ovog
            //this.Credentials.ClientCertificate.Certificate = CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, username);

            factory = this.CreateChannel();
        }

        public void Dispose()
        {
            if (factory != null)
            {
                factory = null;
            }

            this.Close();
        }

        public void Send(Message message)
        {
            try
            {
                factory.Send(message);                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }        
    }
}
