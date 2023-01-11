using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Net;


namespace ClientApp
{
    class Program
    {
        private static int idNum = 0;
        private static EventLog customLog = null;
        const string SourceName = "ClientApp.Program";
        const string LogName = "MySecTest";
        static void Main(string[] args)
        {
            string serviceAddress = "net.tcp://localhost:5000/Chat";
            NetTcpBinding serviceBinding = new NetTcpBinding();

            // Autentifikacija putem Windows autentifikacionog protokola (za komunikaciju sa serverom)
            serviceBinding.Security.Mode = SecurityMode.Transport;
            serviceBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            serviceBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            //Komunikacija sa Service
            using (ServiceProxy serviceProxy = new ServiceProxy(serviceBinding, new EndpointAddress(new Uri(serviceAddress))))
            {
                string username = "";
                User user = null;

                while (user == null)
                {
                    Console.Write("Username: ");
                    username = Console.ReadLine();

                    user = serviceProxy.Connect(username);
                }

                //Logging client connection
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
                if (customLog != null)
                {
                    Console.WriteLine($"{username} successfully Authenticated");
                    customLog.WriteEntry($"{username} successfully Authenticated and CONNECTED to server.");
                }
                else
                {
                    throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log."));
                }


                // Konekcija sa serverom izvrsena
                // Prelazak na autentifikaciju putem sertifikata
                NetTcpBinding chatBinding = new NetTcpBinding();
                chatBinding.Security.Mode = SecurityMode.Transport;
                chatBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
                chatBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                string chatAddress = $"net.tcp://localhost:{5000 + user.Id}/{user.Username}";
                ServiceHost host = new ServiceHost(typeof(ChatService));
                host.AddServiceEndpoint(typeof(IChat), chatBinding, chatAddress);

                //Custom validation mode enables creation of a custom validator - CustomCertificateValidator
                host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
                host.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new CertValidator(user.Username);

                //If CA doesn't have a CRL associated, WCF blocks every client because it cannot be validated
                host.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
                
                ///Set appropriate service's certificate on the host. Use CertManager class to obtain the certificate based on the "srvCertCN"
                
                while (true)
                {
                    host.Credentials.ServiceCertificate.Certificate = CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, $"{user.Username}");
                    Thread.Sleep(2000);
                    if (host.Credentials.ServiceCertificate.Certificate != null) break;                    
                }

                host.Open();
                
                bool isOnline = true;

                while (isOnline)
                {
                    Console.WriteLine("1 - Show users");
                    Console.WriteLine("2 - Send message");
                    Console.WriteLine("3 - Show received messages");
                    Console.WriteLine("4 - Show sent messages");
                    Console.WriteLine("5 - Disconnect");

                    Console.WriteLine("========================");

                    Console.Write("Option: ");
                    string input = Console.ReadLine();

                    Console.WriteLine("========================");

                    switch (input)
                    {
                        case "1":
                            List<User> allUsers = serviceProxy.GetUsers();

                            Console.WriteLine("Chat members:");
                            foreach (User u in allUsers)
                            {
                                Console.WriteLine(u.Username);
                            }

                            break;
                        case "2":
                            List<User> activeUsers = serviceProxy.GetUsers();

                            Console.WriteLine("Chat members:");
                            foreach (User u in activeUsers)
                            {
                                Console.WriteLine(u.Username);
                            }

                            Console.Write("Send to: ");
                            string receiver = Console.ReadLine();

                            Console.Write("Message: ");
                            string text = Console.ReadLine();

                            Common.Message message = new Common.Message()
                            {                                
                                Sender = username,
                                Receiver = receiver,
                                Timestamp= DateTime.Now,
                            };
                            //Autentifikacija preko sertifikata

                            Messages.sent.Add(message);
                            //Messsage encrypting
                            // Create Aes that generates a new key and initialization vector (IV).    
                            // Same key must be used in encryption and decryption    
                            using (AesManaged aes = new AesManaged())
                            {
                                // Encrypt string    
                                byte[] encrypted = Encrypting.Encrypt(text, aes.Key, aes.IV);

                                message.Text = encrypted;
                                message.Key = aes.Key;
                                message.IV = aes.IV;
                            }     

                            int receiverId = activeUsers.FirstOrDefault(u => u.Username == receiver).Id;

                            string clientCertCN = $"{receiver}";
                            /// Use CertificateManager class to obtain the certificate based on the "srvCertCN" representing the expected service identity.
                            X509Certificate2 clientCert = CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, clientCertCN);

                            EndpointAddress receiverAddress = new EndpointAddress(new Uri($"net.tcp://localhost:{5000 + receiverId}/{receiver}"),
                                                  new X509CertificateEndpointIdentity(clientCert));

                            using (ChatProxy chatProxy = new ChatProxy(chatBinding, receiverAddress, receiver, clientCert))
                            {                 
                                
                                chatProxy.Send(message);
                                serviceProxy.Log(message);
                            }

                            break;
                        case "3":
                            Console.WriteLine("Received messages:");
                            foreach (Common.Message m in Messages.received)
                            {
                                Console.WriteLine($"From {m.Sender}: {Encoding.UTF8.GetString(m.Text)}");
                            }
                            break;
                        case "4":
                            Console.WriteLine("Sent messages:");
                            foreach (Common.Message m in Messages.sent)
                            {
                                
                                Console.WriteLine($"To: {m.Receiver}: {Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(Encrypting.Decrypt(m.Text, m.Key, m.IV)))}");
                            }
                            break;
                        case "5":
                            serviceProxy.Disconnect(user);
                            isOnline = false;
                            //Logging the disconnect action
                            try
                            {
                                customLog = new EventLog(LogName,
                                    Environment.MachineName, SourceName);
                            }
                            catch (Exception e)
                            {
                                customLog = null;
                                Console.WriteLine("Error while trying to create log handle. Error = {0}", e.Message);
                            }
                            if (customLog != null)
                            {
                                customLog.WriteEntry($"Client {username} has been DISCONNECTED");

                            }
                            else
                            {
                                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log."));
                            }

                            break;
                        default:
                            return;
                    }

                    Console.WriteLine("========================");
                }

                host.Close();
            }
        }
    }
}
