using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CertValidator : X509CertificateValidator
    {
        //private string subjectName;
        //public CertValidator(string name) 
        //{
        //    this.subjectName = name;
        //}
        /// <summary>
		/// Implementation of a custom certificate validation on the client side.
		/// Client should consider certificate valid if the given certifiate is not self-signed.
		/// If validation fails, throw an exception with an adequate message.
		/// </summary>
		/// <param name="certificate"> certificate to be validate </param>
        public override void Validate(X509Certificate2 certificate)
        {
            /// This will take service's certificate from storage
            //X509Certificate2 srvCert = CertificateManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, subjectName);

            //         if (!certificate.Issuer.Equals(srvCert.Issuer))
            //         {
            //             throw new Exception("Certificate is not from the valid issuer.");
            //         }

            if (certificate.Subject.Equals(certificate.Issuer))
            {
                throw new Exception("Certificate is self-issued.");
            }
        }
    }
}
