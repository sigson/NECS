using NECS.ECS.DefaultsDB.ECSComponents;
using NECS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS_Test.SerializationTest
{
    [TestClass]
    public class SimpleObjectSerializationTest
    {
        [TestMethod]
        public void ObjectSerializationTest()
        {
            EntityManagersComponent component = new EntityManagersComponent();
            var x = DeepCloneExtension.DeepCopyByExpressionTree<EntityManagersComponent>(component);
        }
    }
}
