using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
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

        public ChatProxy(NetTcpBinding binding, EndpointAddress address)
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

        public void Send(Message message)
        {
            try
            {
                message.Timestamp = DateTime.Now;
                factory.Send(message);
                Messages.sent.Add(message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }        
    }
}
