using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding
{
    public sealed class PositionBinder : IComponentBinder
    {
        public Type ComponentType => typeof(Position);

        public void Bind(World w, Entity e, IViewBinder t)
        {
        }

        public void Apply(World w, Entity e, object value, IViewBinder t)
        {
            switch (t)
            {
                case IUnityViewBinder one:
                    one.Go.transform.position = ((Position)value).Value;
                    break;
            }
        }

        public void Unbind(World w, Entity e, IViewBinder t)
        {
        }
    }
}
