using NECS.Extensions;
using NECS.Harness.Services;

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

        
        public void InitWorldScope(Func<Type, bool> staticContractFiltering)
        {
            entityManager = new ECSEntityManager(this);
            componentManager = new ECSComponentManager(this);
            contractsManager = new ECSContractsManager(this, staticContractFiltering);
            ECSService.instance.eventManager.InitializeEventManager();
            contractsManager.InitializeSystems();
            var timer = new TimerCompat(5, (obj, arg) => contractsManager.RunTimeDependContracts(), true);
            timer.Start();
        }
    }
}