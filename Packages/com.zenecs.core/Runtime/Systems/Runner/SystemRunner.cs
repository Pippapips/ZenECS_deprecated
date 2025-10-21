/*
    ZenECS.Core
    수명주기
    MIT | © 2025 Pippapips Limited
*/
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Systems
{
    public enum StructuralFlushPolicy
    {
        EndOfSimulation,  // 기본값: Simulation 그룹 끝에서 즉시 플러시
        BeginOfNextFrame, // 다음 프레임 시작에 플러시
        Manual            // 엔진 자동 호출 없음(테스트/특수 파이프라인용)
    }

    public sealed class SystemRunnerOptions
    {
        /// <summary>Structural changes flush timing.</summary>
        public StructuralFlushPolicy FlushPolicy { get; set; } = StructuralFlushPolicy.EndOfSimulation;

        /// <summary>Presentation 구간에서 쓰기 금지 가드 사용 여부.</summary>
        public bool GuardWritesInPresentation { get; set; } = true;

        /// <summary>기본 생성자 (기존 동작과 동일).</summary>
        public SystemRunnerOptions() { }

        /// <summary>필요 옵션을 바로 지정하는 생성자.</summary>
        public SystemRunnerOptions(
            StructuralFlushPolicy flushPolicy = StructuralFlushPolicy.EndOfSimulation,
            bool guardWritesInPresentation = true)
        {
            FlushPolicy = flushPolicy;
            GuardWritesInPresentation = guardWritesInPresentation;
        }

        /// <summary>편의용 기본 인스턴스.</summary>
        public static readonly SystemRunnerOptions Default = new();
    }

    public sealed class SystemRunner
    {
        public SystemRunnerOptions Options => _opt;

        private readonly World _w;
        private IMessageBus _bus;
        private readonly SystemPlanner.Plan? _plan;
        private readonly SystemRunnerOptions _opt;
        private bool _pendingFlush; // 다음 프레임 시작 시 플러시할지 표시
        private bool _started;
        private bool _stopped;

        public SystemRunner(World w,
            IMessageBus? bus = null,
            IEnumerable<ISystem>? systems = null,
            SystemRunnerOptions? opt = null,
            Action<string>? warn = null)
        {
            _bus = bus ?? new MessageBus();
            _w = w ?? throw new ArgumentNullException(nameof(w));
            _plan = SystemPlanner.Build(systems, warn);
            _opt = opt ?? new SystemRunnerOptions();
        }

        public void InitializeSystems()
        {
            if (_started)
            {
                return;
            }
            _started = true;

            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleInitializeOrder)
                {
                    s.Initialize(_w);
                }
            }
            _w.RunScheduledJobs();
        }

        // 2) 종료: 역순(Pres → Sim → Init)
        public void ShutdownSystems()
        {
            if (!_started || _stopped)
            {
                return;
            }
            _stopped = true;

            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleShutdownOrder)
                {
                    s.Shutdown(_w);
                }
            }
        }

        /// <summary>Unity FixedUpdate 대응: 고정 스텝 블록. 구조 변경은 기록만, 적용은 정책에 따름.</summary>
        public void FixedStep(float fixedDelta)
        {
            // 고정 스텝 기준 준비가 필요하면 Init 포함 가능 (dt 사용 금지)
            RunFixedGroup(SystemGroup.FrameSetup);

            // 고정 스텝 dt 주입
            _w.DeltaTime = fixedDelta;

            RunFixedGroup(SystemGroup.Simulation);
            // NOTE: 여기서는 RunScheduledJobs() 호출하지 않음 (배리어는 BeginFrame에서)
        }

        /// <summary>Unity Update 대응: 프레임 시작(가변 스텝 + 배리어).</summary>
        public void BeginFrame(float deltaTime)
        {
            // "다음 프레임 시작" 정책이면 이전 프레임 예약분을 여기서 적용
            if (_opt.FlushPolicy == StructuralFlushPolicy.BeginOfNextFrame && _pendingFlush)
            {
                _w.RunScheduledJobs();
                _pendingFlush = false;
            }

            _bus.PumpAll();

            // Init: 입력 스냅샷/큐 스왑 등 (dt 금지)
            RunGroup(SystemGroup.FrameSetup);

            _w.RunScheduledJobs();

            // 가변 스텝 dt 주입
            _w.DeltaTime = deltaTime;

            RunGroup(SystemGroup.Simulation);

            // 배리어 정책
            if (_opt.FlushPolicy == StructuralFlushPolicy.EndOfSimulation)
            {
                _w.RunScheduledJobs(); // 표준 위치(프레젠테이션 직전)
            }
            else if (_opt.FlushPolicy == StructuralFlushPolicy.BeginOfNextFrame)
            {
                _pendingFlush = true; // 다음 프레임 시작에 적용
            }
        }

        /// <summary>Unity LateUpdate 대응: 표시(Read-only) 단계.</summary>
        public void LateFrame(float interpolationAlpha = 1f)
        {
            using IDisposable? guard = _opt.GuardWritesInPresentation ? DenyWrites() : null;
            RunLateGroup(SystemGroup.Presentation);

            _w.FrameCount++;
        }

        private static DisposableAction DenyWrites()
        {
            Func<World, Entity, Type, bool>? prev = EcsActions.WritePermissionHook;
            EcsActions.SetWritePermission((w, e, t) => false);
            return new DisposableAction(() => EcsActions.SetWritePermission(prev));
        }

        private sealed class DisposableAction : IDisposable
        {
            private readonly Action _onDispose;
            public DisposableAction(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }

        private void RunFixedGroup(SystemGroup g)
        {
            if (_plan == null)
            {
                return;
            }

            switch (g)
            {
                case SystemGroup.FrameSetup:
                {
                    foreach (IFixedSetupSystem s in _plan.FrameSetup.OfType<IFixedSetupSystem>())
                    {
                        s.Run(_w);
                    }
                    break;
                }
                case SystemGroup.Simulation:
                {
                    foreach (IFixedRunSystem s in _plan.Simulation.OfType<IFixedRunSystem>())
                    {
                        s.Run(_w);
                    }
                    break;
                }
            }
        }

        private void RunGroup(SystemGroup g)
        {
            if (_plan == null)
            {
                return;
            }

            switch (g)
            {
                case SystemGroup.FrameSetup:
                {
                    foreach (IFrameSetupSystem s in _plan.FrameSetup.OfType<IFrameSetupSystem>())
                    {
                        s.Run(_w);
                    }
                    break;
                }
                case SystemGroup.Simulation:
                {
                    foreach (IVariableRunSystem s in _plan.Simulation.OfType<IVariableRunSystem>())
                    {
                        s.Run(_w);
                    }
                    break;
                }
            }
        }

        private void RunLateGroup(SystemGroup g, float interpolationAlpha = 1.0f)
        {
            if (_plan == null)
            {
                return;
            }

            if (g == SystemGroup.Presentation)
            {
                foreach (IPresentationSystem s in _plan.Presentation.OfType<IPresentationSystem>())
                {
                    s.Run(_w, interpolationAlpha);
                }
            }
        }
    }
}
