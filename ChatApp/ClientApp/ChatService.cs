using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    public class ChatService : IChat
    {
        public void Send(Message message)
        {
            // Targeted user received message here
            Console.WriteLine($"Encrypted message received: { Encoding.UTF8.GetString(message.Text)}");
            message.Text = Encoding.UTF8.GetBytes(
                Encrypting.Decrypt(message.Text, message.Key, message.IV));

            Messages.received.Add(message);
        }
    }
}
