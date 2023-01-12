using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringServiceApp
{
    public class MonitoringService : IMonitoringService
    {
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
