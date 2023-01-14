﻿using Common;
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

            string monitoringAddress = "net.tcp://localhost:5001/Monitoring";
            NetTcpBinding monitoringBinding = new NetTcpBinding();

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

                string chatAddress = $"net.tcp://localhost:{5001 + user.Id}/{user.Username}";
                ServiceHost host = new ServiceHost(typeof(ChatService));
                host.AddServiceEndpoint(typeof(IChat), chatBinding, chatAddress);

                host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
                host.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

                string workingDirectory = Environment.CurrentDirectory;
                string projectDirectory = Path.Combine(Directory.GetParent(workingDirectory).FullName, @"Common\Certificates");

                // Install TestCA.cer certificate in CurrentUser/TrustedRoot
                string testCertificatePath = Path.Combine(projectDirectory, "TestCA.cer");
                X509Certificate2 testCertificate = CertificateManager.GetCertificateFromFile(testCertificatePath);
                CertificateManager.InstallCertificate(testCertificate, StoreName.AuthRoot, StoreLocation.LocalMachine);

                // Install client's .pfx certificate in CurrentUser/Personal
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
                
                CertificateManager.InstallCertificate(clientCertificate, StoreName.My, StoreLocation.LocalMachine);
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

                            workingDirectory = Environment.CurrentDirectory;
                            projectDirectory = Path.Combine(Directory.GetParent(workingDirectory).FullName, @"Common\Certificates");
                            
                            // Install receiver's .pfx certificate in CurrentUser/TrustedPeople
                            string receiverCertificatePath = Path.Combine(projectDirectory, $"{receiver}.cer");
                            X509Certificate2 receiverCertificate = CertificateManager.GetCertificateFromFile(receiverCertificatePath);
                            CertificateManager.InstallCertificate(receiverCertificate, StoreName.TrustedPeople, StoreLocation.LocalMachine);
                            
                            EndpointAddress receiverAddress = new EndpointAddress(new Uri($"net.tcp://localhost:{5001 + receiverId}/{receiver}"),
                                                  new X509CertificateEndpointIdentity(receiverCertificate));

                            using (ChatProxy chatProxy = new ChatProxy(chatBinding, receiverAddress, username))
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
