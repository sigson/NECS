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
    [System.Serializable]
    [TypeUid(6)]
    public class ECSComponentGroup : ECSComponent
    {
        public static new long Id { get; set; } = 6;
    }
}
