using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Adapter.Unity.Sync.Targets;
using ZenECS.Core;
using ZenECS.Core.Sync;

namespace ZenECS.Adapter.Unity.Sync.Handlers
{
    public sealed class PositionHandler : ISyncHandler
    {
        public Type ComponentType => typeof(Position);

        public void Bind(World w, Entity e, ISyncTarget t)
        {
        }

        public void Apply(World w, Entity e, object value, ISyncTarget t)
        {
            switch (t)
            {
                case IViewGroupTarget g:
                {
                    var items = g.Items;
                    foreach (var item in items)
                    {
                        item.Go.transform.position = ((Position)value).Value;
                    }
                    break;
                }
                case IViewTarget one:
                    one.Go.transform.position = ((Position)value).Value;
                    break;
            }
        }

        public void Unbind(World w, Entity e, ISyncTarget t)
        {
        }
    }
}
