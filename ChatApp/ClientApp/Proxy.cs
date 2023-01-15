using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
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
        private static EventLog customLog = null;
        const string SourceName = "ClientApp.ChatProxy";
        const string LogName = "MySecTest";

        public ChatProxy(NetTcpBinding binding, EndpointAddress address, X509Certificate2 certIssuer)
            : base(binding, address)
        {
            string cltCertCN = Common.Formatter.ParseName(WindowsIdentity.GetCurrent().Name);

            this.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
            this.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            
            X509Certificate2 clientCertificate
                = CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, cltCertCN);
            
            try
            {
                ChainValidator.ValidateCert(clientCertificate, certIssuer);
                this.Credentials.ClientCertificate.Certificate = clientCertificate;
                factory = this.CreateChannel();
            }
            catch (Exception e)
            {
                EventLogging();
                if (clientCertificate.Issuer != "CN=TestCA")
                {
                    CertificateManager.ResetCertificate(clientCertificate);
                    customLog.WriteEntry($"Certificate {clientCertificate.GetName().Remove(0, 3)} has been remade by different issuer {clientCertificate.Issuer}");
                }
                else
                {
                    CertificateManager.ResetCertificate(certIssuer);
                    customLog.WriteEntry($"Certificate {certIssuer.GetName().Remove(0, 3)} has been remade by different issuer {certIssuer.Issuer}");
                }                
                Console.WriteLine(e);
                this.Close();
            }          
        }
        public void EventLogging()
        {            
            try
            {
                if (!EventLog.SourceExists(SourceName))
                {
                    EventLog.CreateEventSource(SourceName, LogName);
                }
                customLog = new EventLog(LogName,
                    Environment.MachineName, SourceName);
            }
            catch (Exception e)
            {
                customLog = null;
                Console.WriteLine("Error while trying to create log handle. Error = {0}", e.Message);
            }
            if (customLog == null)
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log."));
            }
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
