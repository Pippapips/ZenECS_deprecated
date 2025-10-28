#nullable enable
using System;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Messaging;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Unity ↔ ZenECS 브릿지. 씬에 한 개 두고 커널을 부팅/드라이브/정리한다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class EcsDriver : MonoBehaviour
    {
        void Start()
        {
        }

        void Update()
        {
            if (!EcsKernel.IsRunning) return;
            EcsKernel.BeginFrame(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!EcsKernel.IsRunning) return;
            EcsKernel.FixedStep(Time.fixedDeltaTime);
        }
        
        void LateUpdate()
        {
            if (!EcsKernel.IsRunning) return;
            EcsKernel.LateFrame();
        }
    }
}
