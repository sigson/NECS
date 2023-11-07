using NECS;
using NECS.ECS.DefaultsDB.ECSComponents;
using NECS.ECS.ECSCore;
using NECS.ECS.Types.AtomicType;
using NECS.Extensions;
using Newtonsoft.Json;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization.Formatters.Binary;
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
            List< ECSComponentSerializationCheck > arrayCheck = new List<ECSComponentSerializationCheck>();
            for (int i = 0; i < 5000; i++)
            {
                arrayCheck.Add(new ECSComponentSerializationCheck() { RandomString = new Random().RandomString(128), points = new List<ECSComponentSerializationCheck> { new ECSComponentSerializationCheck() { RandomString = new Random().RandomString(128) } } });
                //arrayCheck.Add(new ECSComponentSerializationCheck() { RandomString = new Random().RandomString(128), points = new List<WorldPoint> { new WorldPoint() { Position = new Vector3S() { x = Random.Shared.NextSingle(), y = Random.Shared.NextSingle(), z = Random.Shared.NextSingle() } } } });
            }

            var watch = new System.Diagnostics.Stopwatch();

            var serializer = JsonSerializer.Create(CachingSettings.Default);

            List<byte[]> protobuf = new List<byte[]>();
            int protosizepackets = 0;

            watch.Start();

            foreach (var comp in arrayCheck)
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, comp);
                    //var protoser = EntitySerialization.SerializationSchemaStorage[typeof(ECSComponentSerializationCheck)];
                    //protoser.Serialize(memoryStream, comp);
                    var array = memoryStream.ToArray();
                    protobuf.Add(array);
                    protosizepackets += array.Length;
                }
            }

            //foreach (var comp in arrayCheck)
            //{
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        var protoser = EntitySerialization.SerializationSchemaStorage[typeof(ECSComponentSerializationCheck)];
            //        protoser.Serialize(memoryStream, comp);
            //        var array = memoryStream.ToArray();
            //        protobuf.Add(array);
            //        protosizepackets += array.Length;
            //    }
            //}
            watch.Stop();

            var serializationprotoelapsed = watch.ElapsedMilliseconds;

            watch.Reset();

            List<string> json = new List<string>();
            int jsonsizepackets = 0;
            watch.Start();
            foreach (var comp in arrayCheck)
            {
                using (StringWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, comp);
                    var str = writer.ToString();
                    json.Add(str);
                    jsonsizepackets += str.Length;
                }
            }
            watch.Stop();

            var serializationjsonelapsed = watch.ElapsedMilliseconds;

            watch.Reset();

            

            var serializationbinaryelapsed = watch.ElapsedMilliseconds;
        }
    }
    [Serializable]
    public class ECSComponentSerializationCheck : ECSComponent
    {
        public string RandomString = "";
        public List<ECSComponentSerializationCheck> points = new List<ECSComponentSerializationCheck>();
    }
}
