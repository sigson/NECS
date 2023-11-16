using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.DB.SQLite
{
    public class SQLiteDBProvider : IDBProvider
    {
        public override T CreateUser<T>(T dataRow)
        {
            throw new NotImplementedException();
        }

        public override bool EmailAvailable(string email)
        {
            throw new NotImplementedException();
        }

        public override List<T> ExecuteQuery<T>(string query)
        {
            throw new NotImplementedException();
        }

        public override List<string> GetEmailList()
        {
            throw new NotImplementedException();
        }

        public override T GetUserViaCallsign<T>(string username)
        {
            throw new NotImplementedException();
        }

        public override T GetUserViaEmail<T>(string email)
        {
            throw new NotImplementedException();
        }

        public override void Load(string DBPath)
        {
            throw new NotImplementedException();
        }

        public override bool LoginCheck(string username, long hashedPassword)
        {
            throw new NotImplementedException();
        }

        public override bool SetEmail(long uid, string email)
        {
            throw new NotImplementedException();
        }

        public override bool SetEmailVerified(long uid, bool value)
        {
            throw new NotImplementedException();
        }

        public override bool SetHardwareId(long uid, string hardwareId)
        {
            throw new NotImplementedException();
        }

        public override bool SetHashedPassword(long uid, string hashedPassword)
        {
            throw new NotImplementedException();
        }

        public override bool SetUsername(long uid, string newUsername)
        {
            throw new NotImplementedException();
        }

        public override bool UsernameAvailable(string username)
        {
            throw new NotImplementedException();
        }
    }
}
