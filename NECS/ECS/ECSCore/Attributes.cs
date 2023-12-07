using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace NECS.ECS.ECSCore
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class NetworkScore : Attribute
    {
        public int Score { get; set; }

        public NetworkScore(int score)
        {
            Score = score;
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class LowLevelNetworkEvent : Attribute
    {
        public LowLevelNetworkEvent()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class TypeUidAttribute : Attribute
    {
        public int Id { get; set; }

        public TypeUidAttribute(int id)
        {
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ServerOnlyDataAttribute : Attribute
    {
        public ServerOnlyDataAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ProtocolOptionalAttribute : Attribute
    {
        public ProtocolOptionalAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ProtocolNameAttribute : Attribute
    {
        public string Name { get; }

        public ProtocolNameAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServiceAttribute : Attribute
    {
        public ServiceAttribute() { }
    }

}
