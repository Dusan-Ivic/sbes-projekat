using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string serviceAddress = "net.tcp://localhost:5000/Chat";
            NetTcpBinding serviceBinding = new NetTcpBinding();

            // Autentifikacija putem Windows autentifikacionog protokola (za komunikaciju sa serverom)
            // TODO - Dodati binding-e iz Vezbe 1

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

                // Konekcija sa serverom izvrsena
                // Prelazak na autentifikaciju putem sertifikata
                // TODO - Podesiti binding-e iz Vezbe 3

                string chatAddress = $"net.tcp://localhost:{5000 + user.Id}/{user.Username}";
                NetTcpBinding chatBinding = new NetTcpBinding();

                ServiceHost host = new ServiceHost(typeof(ChatService));
                host.AddServiceEndpoint(typeof(IChat), chatBinding, chatAddress);

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

                            Message message = new Message()
                            {
                                Sender = username,
                                Text = text,
                                Receiver = receiver
                            };
                            
                            int receiverId = activeUsers.FirstOrDefault(u => u.Username == receiver).Id;

                            string receiverAddress = $"net.tcp://localhost:{5000 + receiverId}/{receiver}";

                            using (ChatProxy chatProxy = new ChatProxy(chatBinding, new EndpointAddress(new Uri(receiverAddress))))
                            {
                                chatProxy.Send(message);
                                serviceProxy.Log(message);
                            }

                            break;
                        case "3":
                            Console.WriteLine("Received messages:");
                            foreach (Message m in Messages.received)
                            {
                                Console.WriteLine($"From {m.Sender}: {m.Text}");
                            }
                            break;
                        case "4":
                            Console.WriteLine("Sent messages:");
                            foreach (Message m in Messages.sent)
                            {
                                Console.WriteLine($"To: {m.Receiver}: {m.Text}");
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
