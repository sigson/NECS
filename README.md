# NECS - Nextgen Entity Component System
NECS - full client-server ECS implementation with incremental entities synchronization and simple scalable delimitate data exchange between entities based on simple GDAP(GroupDataAccessPolicy) interfaces. Fast entity serialization based on https://github.com/tomba/netserializer, show perfomance 3x faster then NewtonsoftJson serialization and 9x faster then BinaryFormatter serialization, and optimize traffic size by exclude fields name(in future may be implemented traffic zipping). ECS objects packer mechanism provide high efficient caching techniques for reduce serialization costs even more.<br>
Restrictions:<br><br>
1. Not serialize concurrent types and any other collection types including inherit types of List<> and Dictionary<,>, serialize only List<> and Dictionary<,>. For syncronize access to non thread-safe collections (List<> and Dictionary) was realized extension class InterlockedCollection with integration to core ECS serialization mechanism.<br>
2. Not serialize properties, but was relized methods for signalize object about serialization start and you can cache property values in code of serializable ecs object.<br><br>
In project included network realization from https://github.com/chronoxor/NetCoreServer.

