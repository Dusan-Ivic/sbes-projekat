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
            string sreviceCertCN = "serviceApp";

            /// Use CertManager class to obtain the certificate based on the "srvCertCN" representing the expected service identity.
            X509Certificate2 sreviceCert = CertificateManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, sreviceCertCN);

            EndpointAddress serviceAddress = new EndpointAddress(new Uri("net.tcp://localhost:5000/Chat"),
                                      new X509CertificateEndpointIdentity(sreviceCert));
            NetTcpBinding serviceBinding = new NetTcpBinding();

            // Autentifikacija putem Windows autentifikacionog protokola (za komunikaciju sa serverom)
            // TODO - Dodati binding-e iz Vezbe 1
            serviceBinding.Security.Mode = SecurityMode.Transport;
            serviceBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            serviceBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;


            using (ServiceProxy serviceProxy = new ServiceProxy(serviceBinding, serviceAddress))
            {
                const string clientCertCN = "clientApp";
                serviceProxy.Credentials.ClientCertificate.Certificate = CertificateManager.GetCertificateFromStorage(
                    StoreName.My, StoreLocation.LocalMachine, clientCertCN);

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
                // TODO - Podesiti binding-e iz Vezbe 3

                string chatAddress = $"net.tcp://localhost:{5000 + user.Id}/{user.Username}";

                NetTcpBinding chatBinding = new NetTcpBinding();

                chatBinding.Security.Mode = SecurityMode.Transport;
                chatBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
                chatBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                ServiceHost host = new ServiceHost(typeof(ChatService));

                host.AddServiceEndpoint(typeof(IChat), chatBinding, chatAddress);

                host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = 
                    System.ServiceModel.Security.X509CertificateValidationMode.ChainTrust;
                host.Credentials.ClientCertificate.Authentication.RevocationMode =
                    X509RevocationMode.NoCheck;
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
                                Receiver = receiver
                            };

                            //Messsage encrypting
                            // Create Aes that generates a new key and initialization vector (IV).    
                            // Same key must be used in encryption and decryption    
                            using (AesManaged aes = new AesManaged())
                            {
                                // Encrypt string    
                                byte[] encrypted = Encrypting.Encrypt(text, aes.Key, aes.IV);
                                // Print encrypted string    
                                Console.WriteLine($"Encrypted data: {System.Text.Encoding.UTF8.GetString(encrypted)}");

                                message.Text = encrypted;
                                message.Key = aes.Key;
                                message.IV = aes.IV;
                            }     

                            int receiverId = activeUsers.FirstOrDefault(u => u.Username == receiver).Id;


                            EndpointAddress receiverAddress = new EndpointAddress(new Uri($"net.tcp://localhost:{5000 + receiverId}/{receiver}"),
                                                  new X509CertificateEndpointIdentity(sreviceCert));

                            using (ChatProxy chatProxy = new ChatProxy(chatBinding, receiverAddress))
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
                                Console.WriteLine($"To: {m.Receiver}: {Encoding.UTF8.GetString(m.Text)}");
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
