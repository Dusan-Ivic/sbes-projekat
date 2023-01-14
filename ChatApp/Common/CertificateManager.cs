using System.IO;
using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;

namespace Common
{
    public static class CertificateManager
    {
        /// <summary>
        /// Get a certificate with the specified subject name from the predefined certificate
        /// storage. Only valid certificates should be considered
        /// </summary>
        /// <returns>The requested certificate. If no valid certificate is found, returns null.</returns>
        public static X509Certificate2 GetCertificateFromStorage(StoreName storeName, StoreLocation storeLocation, string subjectName)
        {
            var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, true);
            /// Check whether the subjectName of the certificate is exactly the same as the given "subjectName"
            foreach (X509Certificate2 cert in certCollection)
            {
                if (cert.SubjectName.Name.Equals(string.Format("CN={0}", subjectName)))
                {
                    return cert;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a certificate from the specified .pfx file
        /// </summary>
        /// <param name="fileName">.pfx file name</param>
        /// <returns>The requested certificate. If no valid certificate is found, returns null.</returns>
        public static X509Certificate2 GetCertificateFromFile(string fileName, string password = "ftn")
        {
            try
            {
                var securePassword = ConvertToSecureString(password);
                return new X509Certificate2(fileName, securePassword);
            }
            catch { return null; }
            
        }

        public static void GenerateClientCertificate(string username)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            // This will get the current WORKING directory (\bin\Debug)
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Path.Combine(Directory.GetParent(workingDirectory).FullName, @"Common\Certificates");
            startInfo.WorkingDirectory = projectDirectory;
            string certPath = Path.Combine(projectDirectory, $"{username}.pfx");
            process.StartInfo = startInfo;
            process.Start();

            using (StreamWriter sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine($"makecert -sv {username}.pvk -iv TestCA.pvk -n \"CN={username}\" -pe -ic TestCA.cer {username}.cer " +
                        "-sr localmachine -ss My -sky exchange");
                    sw.WriteLine($"pvk2pfx.exe /pvk {username}.pvk /pi ftn /spc {username}.cer /pfx {username}.pfx");
                }
            }
        }

        public static void InstallCertificate(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
        {
            using (X509Store store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();
            }
        }

        public static SecureString ConvertToSecureString(string str)
        {
            var secPwd = new SecureString();

            foreach (char c in str)
            {
                secPwd.AppendChar(c);
            }

            return secPwd;
        }
    }
}