using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.ECS.ECSCore;

namespace NECS.ECS.Components.ECSComponents
{
    [System.Serializable]
    [TypeUid(13)]
    public class TimerSelfDestructionComponent : TimerComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        public TimerSelfDestructionComponent() { }
        public TimerSelfDestructionComponent(float timeSelfDestruction, Func<ECSEntity, bool> SelfDestructionCondition, Action<ECSEntity> selfDestructAction = null)
        {
            timerAwait = timeSelfDestruction * 1000;
            onEnd = (entity, selfDestructComponent) =>
            {
                if(SelfDestructionCondition(entity))
                {
                    if(selfDestructAction == null)
                    {
                        ManagerScope.instance.entityManager.OnRemoveEntity(entity);
                    }
                    else
                    {
                        selfDestructAction(entity);
                    }
                    TimerStop();
                }
            };
        }

        protected override void OnAdded(ECSEntity entity)
        {
            base.OnAdded(entity);
            this.TimerStart(timerAwait, entity, false, true);
        }
    }
}
