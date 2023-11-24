using NECS.DB.SQLite;
using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UTanksServer.Network.Simple.Net;

namespace NECS.Harness.Services
{
    public class DBService : IService
    {
        private static DBService cacheInstance;
        public static DBService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<DBService>();
                return cacheInstance;
            }
        }

        public string DBPath = "";
        public string DBType = "";
        public IDBProvider DBProvider = null;

        public override void InitializeProcess()
        {
            DBPath = ConstantService.instance.GetByConfigPath("socket").GetObject<string>("DataBase/DBPath");
            DBType = ConstantService.instance.GetByConfigPath("socket").GetObject<string>("Networking/DBType");
            switch (DBType.ToLower())
            {
                case "sqlite":
                    DBProvider = new SQLiteDefaultDBProvider();
                    break;
            }
            DBProvider.Load(DBPath);
        }

        public override void OnDestroyReaction()
        {

        }

        public override void PostInitializeProcess()
        {

        }
    }
}
