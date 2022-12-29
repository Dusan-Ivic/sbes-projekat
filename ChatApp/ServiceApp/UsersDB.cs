using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceApp
{
    public class UsersDB
    {
        private static int idCounter = 0;

        public static Dictionary<string, User> users = new Dictionary<string, User>();

        public static User InsertUser(string username)
        {
            User user = new User()
            {
                Id = ++idCounter,
                Username = username
            };

            users.Add(username, user);

            return user;
        }
    }
}
