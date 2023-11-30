using NECS.DB.SQLite;
using NECS.Harness.Model;

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
            DBPath = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<string>("DataBase/DBPath");
            DBType = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<string>("DataBase/DBType");
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                switch (DBType.ToLower())
                {
                    case "sqlite":
                        DBProvider = new SQLiteDefaultDBProvider();
                        break;
                }
                DBProvider.Load(DBPath);
            }
        }

        public override void OnDestroyReaction()
        {

        }

        public override void PostInitializeProcess()
        {

        }
    }
}
