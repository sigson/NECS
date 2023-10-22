using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public interface IEngineApiCallableMethods
    {
        void Awake();

        void OnEnable();

        void Reset();

        void Start();

        void FixedUpdate();
        void FixedUpdate(double delta);

        void OnTriggerEnter(EngineApiCollider3D other);

        void OnTriggerStay(EngineApiCollider3D other);

        void OnTriggerExit(EngineApiCollider3D other);

        void OnTriggerEnter2D(EngineApiCollider2D other);

        void OnTriggerStay2D(EngineApiCollider2D other);

        void OnTriggerExit2D(EngineApiCollider2D other);

        void OnCollisionEnter(EngineApiCollision3D collision);

        void OnCollisionStay(EngineApiCollision3D collision);

        void OnCollisionExit(EngineApiCollision3D collision);

        void OnCollisionEnter2D(EngineApiCollision2D collision);

        void OnCollisionStay2D(EngineApiCollision2D collision);

        void OnCollisionExit2D(EngineApiCollision2D collision);

        void Update(double delta);
        void Update();

        void LateUpdate();
        void LateUpdate(double delta);

        void OnRenderObject();

        void OnApplicationPause(bool pause);

        void OnApplicationQuit();

        void OnApplicationFocus(bool focus);

        void OnDisable();

        void OnDestroy();
    }
}
