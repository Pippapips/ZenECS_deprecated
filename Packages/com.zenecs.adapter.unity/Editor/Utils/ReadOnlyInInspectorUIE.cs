using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ZenECS.Adapter.Unity.Attributes;

namespace ZenECS.EditorUtils
{
    [CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute), useForChildren: true)]
    public sealed class ReadOnlyInInspector : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property);
            field.SetEnabled(false);
            return field;
        }
    }
}