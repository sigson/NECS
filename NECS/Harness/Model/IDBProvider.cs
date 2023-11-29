using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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
        public abstract bool LoginCheck(string username, string hashedPassword);
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

    public class UserDataRowBase
    {
        //public int id;
        public string Username = "";
        public string Password = "";
        public string Email = "";
        public bool EmailVerified = false;
        public string HardwareId = "";
        public string RegistrationDate = "";
        public string UserPrivilegesGroup = "";
        public string LastIp = "";
        public bool TermlessChatBan = false;
        public bool TermlessBan = false;
        public string UserLocation = "";
        public int Karma = 0;
        public string GameDataPacked = "";

        /// <summary>
        /// item1 = columns, item2 = rows
        /// </summary>
        /// <returns></returns>
        public virtual (List<string>, List<string>) PrepareToDBInsert()
        {
            List<string> columns = new List<string>()
            {
                "Username", "Password", "Email", "EmailVerified", "HardwareId", "RegistrationDate", "UserPrivilegesGroup", "LastIp", "TermlessChatBan", "TermlessBan", "UserLocation", "Karma", "GameDataPacked"
            };
            List<string> rowValues = new List<string>(){
                Username, Password, Email, EmailVerified.ToString(), HardwareId, RegistrationDate, UserPrivilegesGroup, LastIp, TermlessChatBan.ToString(), TermlessBan.ToString(), UserLocation, Karma.ToString(), GameDataPacked
            };
            return (columns, rowValues);
        }

        public virtual void DBUnpack(DbDataReader dbResult)
        {
            if (dbResult.HasRows)
            {
                //Id = int.Parse(dbResult.GetString("Id"));
                Username = dbResult.GetString("Username");
                Password = dbResult.GetString("Password");
                Email = dbResult.GetString("Email");
                EmailVerified = bool.Parse(dbResult.GetString("EmailVerified"));
                HardwareId = dbResult.GetString("HardwareId");
                RegistrationDate = dbResult.GetString("RegistrationDate");
                UserPrivilegesGroup = dbResult.GetString("UserPrivilegesGroup");
                LastIp = dbResult.GetString("LastIp");
                TermlessChatBan = bool.Parse(dbResult.GetString("TermlessChatBan"));
                TermlessBan = bool.Parse(dbResult.GetString("TermlessBan"));
                UserLocation = dbResult.GetString("UserLocation");
                Karma = int.Parse(dbResult.GetString("Karma"));
                GameDataPacked = dbResult.GetString("GameDataPacked");
            }
        }

        public virtual void PackJsonGameData<T>(T gameDataObject) where T : class
        {
            GameDataPacked = System.Text.Json.JsonSerializer.Serialize<T>(gameDataObject);
        }

        public virtual T UnpackJsonGameData<T>() where T : class
        {
            System.IO.MemoryStream mStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(GameDataPacked));
            var reader = new JsonTextReader(new StreamReader(mStream));
            return JObject.Load(reader).ToObject<T>();
        }

        public virtual T SetupNew<T>() where T : UserDataRowBase
        {
            return (T)this;
        }
    }
}
