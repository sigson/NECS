using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public interface IEngineApiCallableMethods
    {
        public void Awake();

        public void OnEnable();

        public void Reset();

        public void Start();

        public void FixedUpdate();
        public void FixedUpdate(double delta);

        public void OnTriggerEnter(EngineApiCollider3D other);

        public void OnTriggerStay(EngineApiCollider3D other);

        public void OnTriggerExit(EngineApiCollider3D other);

        public void OnTriggerEnter2D(EngineApiCollider2D other);

        public void OnTriggerStay2D(EngineApiCollider2D other);

        public void OnTriggerExit2D(EngineApiCollider2D other);

        public void OnCollisionEnter(EngineApiCollision3D collision);

        public void OnCollisionStay(EngineApiCollision3D collision);

        public void OnCollisionExit(EngineApiCollision3D collision);

        public void OnCollisionEnter2D(EngineApiCollision2D collision);

        public void OnCollisionStay2D(EngineApiCollision2D collision);

        public void OnCollisionExit2D(EngineApiCollision2D collision);

        public void Update(double delta);
        public void Update();

        public void LateUpdate();
        public void LateUpdate(double delta);

        public void OnRenderObject();

        public void OnApplicationPause(bool pause);

        public void OnApplicationQuit();

        public void OnApplicationFocus(bool focus);

        public void OnDisable();

        public void OnDestroy();
    }
}
