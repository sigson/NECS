using NECS;
using NECS.ECS.ECSCore;
using NECS.ECS.Types.AtomicType;
using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS_Test.SerializationTest
{
    [TestClass]
    public class FullECSSerializationTest
    {
        [TestMethod]
        public void FullSerializationCheck()
        {
            IService.RegisterAllServices();
            //ConstantService.instance.SetupConfigs(GlobalProgramState.instance.TechConfigDir);
            var entity = new ECSEntity();
            ManagerScope.instance.entityManager.OnAddNewEntity(entity);
            entity.AddComponent(new ECSComponentSerializationCheck());
            entity.dataAccessPolicies.Add(new ECSComponentSerializationCheckGDAP());

            EntitySerialization.SerializeEntity(entity);
            var serialized = EntitySerialization.BuildSerializedEntityWithGDAP(entity, entity);
            var deserialized = EntitySerialization.Deserialize(serialized);
        }

        [Serializable]
        [TypeUid(1001)]
        public class ECSComponentSerializationCheck : ECSComponent
        {
            static new public long Id { get; set; } = 0;
            public string RandomString = "";
            public List<Vector3S> Vector3Sw = new List<Vector3S>();
            public List<ECSComponentSerializationCheck> points = new List<ECSComponentSerializationCheck>();

            public override void OnAdded(ECSEntity entity)
            {
                RandomString = new Random().RandomString(128);
                points = new List<ECSComponentSerializationCheck> {
                        new ECSComponentSerializationCheck() { RandomString = new Random().RandomString(128) }
                    };
                Vector3Sw = new List<Vector3S>()
                    {
                        new Vector3S()
                        {
                            x = 4440, y = 1240, z = 1230
                        }
                    };
            }
        }
        [Serializable]
        public class ECSComponentSerializationCheckGDAP : GroupDataAccessPolicy
        {
            public ECSComponentSerializationCheckGDAP()
            {
                this.AvailableComponents = new List<long> {
                    ECSComponentSerializationCheck.Id
                };
            }
        }
    }
}
