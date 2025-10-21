namespace ZenECS.Core.Systems
{
    public enum SystemGroup { FrameSetup, Simulation, Presentation }

    public interface ISystem
    {
        void Run(World w);
    }
}
