﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    public class QuaternionS : CachingSerializable
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
        public float w = 0f;
    }
}
