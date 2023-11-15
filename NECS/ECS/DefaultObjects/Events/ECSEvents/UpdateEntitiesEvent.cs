using NECS.ECS.ECSCore;

namespace NECS.ECS.Events.ECSEvents
{
    [Serializable]
    [TypeUid(15)]
    public class UpdateEntitiesEvent : ECSEvent
    {
        static public new long Id { get; set; } = 15;
        public long EntityIdRecipient; //ID of user with socket component
        public List<byte[]> Entities;
        public override void Execute()
        {
            foreach (var entity in Entities)
            {
                EntitySerialization.UpdateDeserialize(entity);
            }
        }
    }
}
