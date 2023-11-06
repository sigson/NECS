using NECS.ECS.DefaultsDB.ECSComponents;
using NECS.ECS.ECSCore;
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
            EntitySerialization.InitSerialize();
        }
    }
}
