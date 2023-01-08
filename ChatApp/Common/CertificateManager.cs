using System.Security;
using System.Security.Cryptography.X509Certificates;

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

            /// Check whether the subjectName of the certificate is exactly the same as the given "subjectName"
            foreach (var cert in store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, validOnly: true))
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
            var securePassword = ConvertToSecureString(password);
            return new X509Certificate2(fileName, securePassword);
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