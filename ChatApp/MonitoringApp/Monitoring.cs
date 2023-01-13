using Common;
using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringApp
{
    // TODO - Implementirati IMonitoring intefejs
    class Monitoring : IMonitoring
    {
        public void Log(Message message)
        {
            string decryptedMessage = Encrypting.Decrypt(message.Text, message.Key, message.IV);
            Console.WriteLine($"[{message.Timestamp}] {message.Sender} -> {message.Receiver}: {decryptedMessage}");

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"messages.txt");

            using (StreamWriter sw = System.IO.File.AppendText(path))
            {
                sw.WriteLine($"{message.Sender} -> {message.Receiver}  \t Message: {decryptedMessage} \t Time: {message.Timestamp}");
            }
        }
    }
}
