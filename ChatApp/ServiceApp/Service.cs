using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceApp
{
    class Service : IService
    {
        public User Connect(string username)
        {
            if (UsersDB.users.ContainsKey(username))
            {
                Console.WriteLine($"User \"{username}\" already exists");
                return null;
            }

            Console.WriteLine($"User \"{username}\" connected");

            return UsersDB.InsertUser(username);
        }

        public void Disconnect(User user)
        {
            UsersDB.users.Remove(user.Username);
        }

        public List<User> GetUsers()
        {
            return UsersDB.users.Values.ToList();
        }

        public void Log(Message message)
        {
            Console.WriteLine($"[{message.Timestamp}] {message.Sender} -> {message.Receiver}: {message.Text}");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"messages.txt");
            DateTime time = DateTime.Now;
            File.WriteAllText(path, $"{message.Sender} -> {message.Receiver}  \t Message: {message.Text} \t Time:{time}");
        }
    }
}
