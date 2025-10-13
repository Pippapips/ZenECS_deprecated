namespace ZenECS.Core.Messaging.Samples
{
    [Command] public struct CmdMove { public int EntityId; public float X, Y, Z; }
    [Event]   public struct HitEvent { public int AttackerId; public int VictimId; public float Damage; }
}