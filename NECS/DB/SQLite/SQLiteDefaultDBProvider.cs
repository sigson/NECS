#if NET && !GODOT
using Microsoft.Data.Sqlite;
using NECS.Core.Logging;
using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

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
                SQLiteDefaultDBProvider.CreateUsersTableQuery + SQLiteDefaultDBProvider.CreateNews + SQLiteDefaultDBProvider.CreateLogs + SQLiteDefaultDBProvider.CreateInvites + SQLiteDefaultDBProvider.CreateFriends, Connection))
                request.ExecuteNonQuery();
        }

        public override T CreateUser<T>(T dataRow)
        {
            if (! EmailAvailable(dataRow.Email)) throw new ArgumentException("Email Taken!");
            var packed = dataRow.PrepareToDBInsert();
            var columns = "";
            var values = "";
            packed.Item1.ForEach(x => columns += x + ", ");
            packed.Item2.ForEach(x => values += "'" + x + "', ");
            columns = columns.Substring(0, columns.Length - 2);
            values = values.Substring(0, values.Length - 2);
            using (SqliteCommand request = new SqliteCommand(
                $"INSERT INTO Users({columns}) VALUES({values});",
                this.Connection
            ))
            {
                request.ExecuteNonQuery();

                return GetUserViaCallsign<T>(dataRow.Username);
            }
        }

        public override bool EmailAvailable(string email)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"SELECT id FROM Users WHERE Email = '{email}';",
                this.Connection
            ))
            {
                DbDataReader response = request.ExecuteReader(CommandBehavior.SingleRow);
                bool result = !response.HasRows;
                response.Close();
                return result;
            }
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
            using (SqliteCommand request = new SqliteCommand(
                $"SELECT * FROM Users WHERE Username = '{username}'",
                this.Connection
            ))
            {
                using (DbDataReader response = request.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!response.HasRows)
                    {
                        return default(T);
                    }

                    response.Read();

                    var dataObject = Activator.CreateInstance<T>();
                    dataObject.DBUnpack(response);
                    return dataObject;
                }
            }
        }

        public override T GetUserViaEmail<T>(string email)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"SELECT * FROM Users WHERE Email = '{email}'",
                this.Connection
            ))
            {
                using (DbDataReader response = request.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!response.HasRows)
                    {
                        return default(T);
                    }

                    response.Read();

                    var dataObject = Activator.CreateInstance<T>();
                    dataObject.DBUnpack(response);
                    return dataObject;
                }
            }
        }

        public override void Load(string DBPath)
        {
            string connectionString = "Data Source=" + PathEx.Combine(GlobalProgramState.instance.GameDataDir, DBPath) + ";Cache=Shared;Mode=ReadWriteCreate;";
            if(Defines.DBEventsLogging)
                NLogger.LogDB($"Using DB on path => '{connectionString}'");
            Connection = new SqliteConnection(connectionString);
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            Connection.Open();
            SetupDatabase();
        }

        public override bool LoginCheck(string username, string hashedPassword)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"SELECT id FROM Users WHERE Username = '{username}' AND Password = '{hashedPassword}' COLLATE NOCASE;",
                this.Connection
            ))
            {
                DbDataReader response = request.ExecuteReader(CommandBehavior.SingleRow);
                bool result = response.HasRows;
                response.Close();
                return result;
            }
        }

        public override bool SetEmail(long uid, string email)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"UPDATE Users SET Email = '{email}', EmailVerified = 0 WHERE id = {uid}",
                this.Connection
            )) return request.ExecuteNonQuery() > 0;
        }

        public override bool SetEmailVerified(long uid, bool value)
        {
            using (SqliteCommand request = new SqliteCommand(
                $"UPDATE Users SET EmailVerified = {(value ? 1 : 0)} WHERE id = {uid}",
                this.Connection
            )) return request.ExecuteNonQuery() > 0;
        }

        public override bool SetHardwareId(long uid, string hardwareId)
        {
            if (hardwareId.Length > 100) throw new ArgumentException("Parameter hardwareId cannot not be over 100 characters");
            using (SqliteCommand request = new SqliteCommand(
                $"UPDATE Users SET HardwareId = '{hardwareId}' WHERE id = {uid}",
                this.Connection
            )) return request.ExecuteNonQuery() > 0;
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
#endif