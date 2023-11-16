using Microsoft.Data.Sqlite;
using NECS.Core.Logging;
using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.DB.SQLite
{
    public class SQLiteDefaultDBProvider : IDBProvider
    {
        #region gamestatic buildDB
        public static string CreateUsersTableQuery = @"
CREATE TABLE IF NOT EXISTS  ""Users"" (
	""id""	INTEGER NOT NULL,
	""Username""	VARCHAR(20) NOT NULL COLLATE NOCASE,
	""Password""	VARCHAR(44) NOT NULL,
	""Email""	TEXT,
    ""EmailVerified""	TINYINT(1) NOT NULL DEFAULT 0,
	""HardwareId""	TEXT NOT NULL,
	""RegistrationDate""	TEXT NOT NULL,
	""UserPrivilegesGroup""	TEXT NOT NULL DEFAULT 'admin',
    ""LastIp""	TEXT NOT NULL,
    ""TermlessChatBan""	TINYINT(1) NOT NULL DEFAULT 0,
    ""TermlessBan""	TINYINT(1) NOT NULL DEFAULT 0,
    ""UserLocation""	TEXT NOT NULL DEFAULT 'en',
	""Karma""	INTEGER NOT NULL DEFAULT 0,
    ""GameDataPacked""	TEXT NOT NULL,
	PRIMARY KEY(""id"" AUTOINCREMENT)
);
";

        static public string CreateNews = @"
CREATE TABLE IF NOT EXISTS ""News"" (
	""id""	INTEGER NOT NULL,
	""Date""	TEXT NOT NULL,
	""Header""	TEXT NOT NULL,
	""Text""	TEXT NOT NULL,
	""Icon""	TEXT NOT NULL DEFAULT 'https://myserver.com/img.jpg',
	PRIMARY KEY(""id"" AUTOINCREMENT)
);
";

        static public string CreateLogs = @"
CREATE TABLE IF NOT EXISTS ""Logs"" (
	""id""	INTEGER NOT NULL,
	""Date""	TEXT NOT NULL,
	""Type""	TEXT NOT NULL,
	""Message""	TEXT NOT NULL,
	PRIMARY KEY(""id"" AUTOINCREMENT)
);
";

        static public string CreateInvites = @"
CREATE TABLE IF NOT EXISTS ""Invites"" (
	""id""	INTEGER NOT NULL,
	""UserId""	INTEGER NOT NULL,
	""Code""	TEXT NOT NULL,
	FOREIGN KEY(""UserId"") REFERENCES ""Users""(""id""),
	PRIMARY KEY(""id"" AUTOINCREMENT)
);
";

        static public string CreateFriends = @"
CREATE TABLE IF NOT EXISTS ""Friends"" (
	""id""	INTEGER NOT NULL,
	""UserId""	INTEGER NOT NULL,
	""FriendId""	INTEGER NOT NULL,
	FOREIGN KEY(""UserId"") REFERENCES ""Users""(""id""),
	FOREIGN KEY(""FriendId"") REFERENCES ""Users""(""id""),
	PRIMARY KEY(""id"" AUTOINCREMENT)
);
";

        #endregion

        SqliteConnection Connection;
        public void SetupDatabase()
        {
            using (SqliteCommand request = new SqliteCommand(
                SQLiteDefaultDBProvider.CreateUsersTableQuery + SQLiteDefaultDBProvider.CreateNews + SQLiteDefaultDBProvider.CreateLogs + SQLiteDefaultDBProvider.CreateInvites + SQLiteDefaultDBProvider.CreateFriends))
                request.ExecuteNonQuery();
        }

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
            using (SqliteCommand request = new SqliteCommand(
                "SELECT Users.Email AS Email FROM Users, `user-settings` WHERE users.uid = `user-settings`.uid AND email != '' AND `user-settings`.subscribed AND Users.EmailVerified;",
                Connection
            ))
            {
                Queue<string> result = new Queue<string>();
                DbDataReader response = request.ExecuteReader(CommandBehavior.Default);

                while (response.HasRows)
                {
                    response.Read();
                    result.Enqueue(response.GetString("Email"));
                    response.NextResult();
                }

                return result.ToList();
            }
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
            string connectionString = "URI=file:" + Path.Join(GlobalProgramState.instance.GameDataDir, DBPath);
            if(Defines.DBEventsLogging)
                Logger.LogDB($"Using DB on path => '{connectionString}'");
            Connection = new SqliteConnection(connectionString);
            Connection.Open();
            SetupDatabase();
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
            if (hashedPassword.Length > 44) throw new ArgumentException("Parameter 'hashedPassword' is too long!");

            using (SqliteCommand request = new SqliteCommand(
                $"UPDATE Users SET Password = '{hashedPassword}' WHERE id = {uid}",
                Connection
            )) return request.ExecuteNonQuery() > 0;
        }

        public override bool SetUsername(long uid, string newUsername)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"UPDATE Users SET Username = '{newUsername}' WHERE id = {uid}",
                Connection
            )) return request.ExecuteNonQuery() > 0;
        }

        public override bool UsernameAvailable(string username)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"SELECT id FROM Users WHERE Username = '{username}' COLLATE NOCASE;",
                Connection
            ))
            {
                DbDataReader response = request.ExecuteReader(CommandBehavior.SingleRow);
                bool result = !response.HasRows;
                response.Close();
                return result;
            }
        }
    }
}
