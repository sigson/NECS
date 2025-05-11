using NECS.Extensions;

namespace NECS.ECS.ECSCore
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class ECSWorld
    {
        public long instanceId = Guid.NewGuid().GuidToLong();
        public string WorldMetaData = "";
        public ECSContractsManager contractsManager;
        public ECSEntityManager entityManager;
        public ECSComponentManager componentManager;
        public ECSEventManager eventManager;

        
        public void InitWorldScope(Func<Type, bool> staticContractFiltering)
        {
            entityManager = new ECSEntityManager(this);
            componentManager = new ECSComponentManager(this);
            eventManager = new ECSEventManager(this);
            eventManager.IdStaticCache();
            contractsManager = new ECSContractsManager(this, staticContractFiltering);
            contractsManager.InitializeSystems();
            eventManager.InitializeEventManager();
            var timer = new TimerCompat(5, (obj, arg) => contractsManager.RunTimeDependContracts(), true);
            timer.Start();
        }
    }
}