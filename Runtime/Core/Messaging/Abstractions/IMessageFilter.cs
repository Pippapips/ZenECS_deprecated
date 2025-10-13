namespace ZenECS.Core.Messaging
{
    public interface IMessageFilter
    {
        bool Allow<T>(in T message);
    }
}