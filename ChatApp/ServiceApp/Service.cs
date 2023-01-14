using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
namespace ServiceApp
{
    class Service : IService
    {
        private static EventLog customLog = null;
        const string SourceName = "ServiceApp.Service";
        const string LogName = "MySecTest";
        public User Connect(string username)
        {
            if (UsersDB.users.ContainsKey(username))
            {
                Console.WriteLine($"User \"{username}\" already exists");
                return null;
            }
            EventLogging();
            Console.WriteLine($"User \"{username}\" connected");

            if(CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, username) ==null)
            {
                CertificateManager.GenerateClientCertificate(username);
                customLog.WriteEntry($"Certificate generated for {username}");
            }
            customLog.WriteEntry($"{username} CONNECTED");
            return UsersDB.InsertUser(username);
        }

        public void Disconnect(User user)
        {
            customLog.WriteEntry($"{user.Username} DISCONNECTED");
            UsersDB.users.Remove(user.Username);
        }

        public List<User> GetUsers()
        {
            return UsersDB.users.Values.ToList();
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
    }
}
