using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServiceApp
{
    public class RevocationList
    {

        public static List<X509Certificate2> invalidCertificates = new List<X509Certificate2>();

    }
}
