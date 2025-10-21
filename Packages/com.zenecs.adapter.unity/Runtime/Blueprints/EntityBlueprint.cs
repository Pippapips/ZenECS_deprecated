using UnityEngine;
using ZenECS.Adapter.Unity.Binding;
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Extensions;

namespace ZenECS.Adapter.Unity.Blueprints
{
    [CreateAssetMenu(menuName = "ZenECS/Entity Blueprint", fileName = "EntityBlueprint")]
    public sealed partial class EntityBlueprint : ScriptableObject
    {
        [SerializeField] private BlueprintData _data = new();
        public BlueprintData Data => _data;

        [Header("Optional View Authoring")]
        public GameObject target;
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;

        public Entity Create(World world, IViewBinderFactory factory)
        {
            var e = world.CreateEntity();
            _data.ApplyTo(world, e);

            var view = target.GetComponent<IViewBinder>();
            if (view != null)
            {
                var viewTarget = factory.Create(target);
                if (viewTarget != null)
                {
                    world.Replace(e, new Position(position));
                    world.Replace(e, new Rotation(rotation));
                    world.Replace(e, new Scale(scale));
                    viewTarget.SetEntity(e);
                }
            }

            return e;
        }
    }
}