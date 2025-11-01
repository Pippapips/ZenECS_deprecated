#nullable enable

namespace ZenECS.Core.Systems
{
    public interface ISystemRunner
    {
        void BeginFrame(float deltaTime);
        void FixedStep(float fixedDelta);
        void LateFrame(float interpolationAlpha = 1f);
    }
}