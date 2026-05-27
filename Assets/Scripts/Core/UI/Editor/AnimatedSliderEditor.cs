#if UNITY_EDITOR
using Core.UI.Animations;
using UnityEditor;
using UnityEditor.UI;

namespace Core.UI.Editor
{

    [CustomEditor(typeof(AnimatedSlider), true)]
    [CanEditMultipleObjects]
    public class AnimatedSliderEditor : SliderEditor
    {
        private SerializedProperty _animationSettings;
        private SerializedProperty _arrows;

        protected override void OnEnable()
        {
            base.OnEnable();

            _animationSettings = serializedObject.FindProperty("animationSettings");
            _arrows = serializedObject.FindProperty("arrows");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Button Animation", EditorStyles.boldLabel);

            serializedObject.Update();

            EditorGUILayout.PropertyField(_animationSettings);

            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(_arrows);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif