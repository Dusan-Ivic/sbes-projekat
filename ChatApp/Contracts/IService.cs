using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        User Connect(string username);

        [OperationContract]
        List<User> GetUsers();

        [OperationContract]
        void Disconnect(User user);

        [OperationContract]
        void Log(Message message);
    }
}
