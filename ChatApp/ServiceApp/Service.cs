using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

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

            //generise sertifikat za klijenta ako vec ne postoji
            if(CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, username) ==null)
            {
                CertificateManager.GenerateClientCertificate(username);
            }
            
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
            string messText = Encrypting.Decrypt(message.Text, message.Key, message.IV);
            Console.WriteLine($"[{message.Timestamp}] {message.Sender} -> {message.Receiver}: {messText}");

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"messages.txt");
            DateTime time = DateTime.Now;

            using (StreamWriter sw = System.IO.File.AppendText(path))
            {
                sw.WriteLine($"{message.Sender} -> {message.Receiver}  \t Message: {messText} \t Time:{time}");
            }            
        }
    }
}
