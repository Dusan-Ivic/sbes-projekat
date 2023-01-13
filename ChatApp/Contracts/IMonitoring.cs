using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    // TODO - Dodati IMonitoring interfejs

    [ServiceContract]
    public interface IMonitoring
    {
        [OperationContract]
        void Log(Message message);
    }
}
