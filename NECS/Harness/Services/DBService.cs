﻿#if NET && !GODOT
using NECS.DB.SQLite;
#endif
using NECS.Harness.Model;

namespace NECS.Harness.Services
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class DBService : IService
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
#if NET && !GODOT
                switch (DBType.ToLower())
                {
                    case "sqlite":
                        DBProvider = new SQLiteDefaultDBProvider();
                        break;
                }
                DBProvider.Load(DBPath);
#endif
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
