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
        static void Main(string[] args)
        {
            string serviceAddress = "net.tcp://localhost:5000/Chat";
            NetTcpBinding serviceBinding = new NetTcpBinding();

            string monitoringAddress = "net.tcp://localhost:5001/Monitoring";
            NetTcpBinding monitoringBinding = new NetTcpBinding();

            serviceBinding.Security.Mode = SecurityMode.Transport;
            serviceBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            serviceBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            //Komunikacija sa Service
            using (ServiceProxy serviceProxy = new ServiceProxy(serviceBinding, new EndpointAddress(new Uri(serviceAddress))))
            {
                string username = Common.Formatter.ParseName(WindowsIdentity.GetCurrent().Name);
                User user = serviceProxy.Connect(username);

                NetTcpBinding chatBinding = new NetTcpBinding();
                chatBinding.Security.Mode = SecurityMode.Transport;
                chatBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
                chatBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                string chatAddress = $"net.tcp://localhost:{5001 + user.Id}/{user.Username}";
                ServiceHost host = new ServiceHost(typeof(ChatService));
                host.AddServiceEndpoint(typeof(IChat), chatBinding, chatAddress);

                host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
                host.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.Offline;

                string workingDirectory = Environment.CurrentDirectory;
                string projectDirectory = Path.Combine(Directory.GetParent(workingDirectory).FullName, @"Common\Certificates");

                string clientCertificatePath = Path.Combine(projectDirectory, $"{username}.pfx");
                X509Certificate2 clientCertificate = null;

                while (clientCertificate == null)
                {
                    try
                    {
                        clientCertificate = CertificateManager.GetCertificateFromFile(clientCertificatePath);
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(1000);
                    }
                }

                host.Credentials.ServiceCertificate.Certificate = clientCertificate;
                
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
                                Timestamp = DateTime.Now,
                            };
                            Messages.sent.Add(message);
                            
                            using (AesManaged aes = new AesManaged())
                            {
                                byte[] encrypted = Encrypting.Encrypt(text, aes.Key, aes.IV);

                                message.Text = encrypted;
                                message.Key = aes.Key;
                                message.IV = aes.IV;
                            }     

                            int receiverId = activeUsers.FirstOrDefault(u => u.Username == receiver).Id;

                            workingDirectory = Environment.CurrentDirectory;
                            projectDirectory = Path.Combine(Directory.GetParent(workingDirectory).FullName, @"Common\Certificates");
                            
                            string receiverCertificatePath = Path.Combine(projectDirectory, $"{receiver}.cer");
                            X509Certificate2 receiverCertificate = CertificateManager.GetCertificateFromFile(receiverCertificatePath);

                            EndpointAddress receiverAddress = new EndpointAddress(new Uri($"net.tcp://localhost:{5001 + receiverId}/{receiver}"),
                                                  new X509CertificateEndpointIdentity(receiverCertificate));

                            using (ChatProxy chatProxy = new ChatProxy(chatBinding, receiverAddress))
                            {
                                chatProxy.Send(message);
                            }
                            
                            using (MonitoringProxy monitoringProxy = new MonitoringProxy(monitoringBinding, new EndpointAddress(monitoringAddress)))
                            {
                                monitoringProxy.Log(message);
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
