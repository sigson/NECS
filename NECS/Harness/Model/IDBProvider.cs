using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IDBProvider
    {
        public abstract void Load(string DBPath);
        public abstract List<T> ExecuteQuery<T>(string query);
        public abstract bool UsernameAvailable(string username);
        public abstract bool LoginCheck(string username, long hashedPassword);
        public abstract bool EmailAvailable(string email);
        public abstract T CreateUser<T>(T dataRow) where T : UserDataRowBase;
        public abstract T GetUserViaCallsign<T>(string username) where T : UserDataRowBase;
        public abstract T GetUserViaEmail<T>(string email) where T : UserDataRowBase;
        public abstract List<string> GetEmailList();
        public abstract bool SetUsername(long uid, string newUsername);
        public abstract bool SetHashedPassword(long uid, string hashedPassword);
        public abstract bool SetEmail(long uid, string email);
        public abstract bool SetEmailVerified(long uid, bool value);
        public abstract bool SetHardwareId(long uid, string hardwareId);
    }

    public abstract class UserDataRowBase
    {
        public int Id;
        public string Username = "";
        public string Password = "";
        public string Email = "";
        public bool EmailVerified = false;
        public string HardwareId = "";
        public string GameDataPacked = "";

        /// <summary>
        /// item1 = columns, item2 = rows
        /// </summary>
        /// <returns></returns>
        public virtual (List<string>, List<string>) PrepareToDBInsert()
        {
            List<string> columns = new List<string>()
            {
                "Id", "Username", "Password", "Email", "EmailVerified", "HardwareId", "GameDataPacked"
            };
            List<string> rowValues = new List<string>(){
                Id.ToString(), Username, Password, Email, EmailVerified.ToString(), HardwareId, GameDataPacked
            };
            return (columns, rowValues);
        }

        public virtual void DBUnpack(DbDataReader dbResult)
        {
            if (dbResult.HasRows)
            {
                Id = int.Parse(dbResult.GetString("Id"));
                Username = dbResult.GetString("Username");
                Password = dbResult.GetString("Password");
                Email = dbResult.GetString("Email");
                EmailVerified = bool.Parse(dbResult.GetString("EmailVerified"));
                HardwareId = dbResult.GetString("Username");
                GameDataPacked = dbResult.GetString("Username");
            }
        }
    }
}
