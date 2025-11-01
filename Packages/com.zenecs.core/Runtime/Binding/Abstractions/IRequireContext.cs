#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Declarative marker: a binder REQUIRES a context T to be registered on
    /// the same Entity before attachment (validated in Router & Inspector).
    /// </summary>
    public interface IRequireContext<T> where T : class, IContext { }
}