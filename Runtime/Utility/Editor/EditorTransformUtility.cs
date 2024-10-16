#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HollowEditor
{
    public static class EditorTransformUtility
    {
        public static void MoveTransforms(Transform[] transforms, Transform activeTransform, Vector3 position, PivotMode pivotMode)
        {
            Vector3 pivotPoint;
            if (pivotMode == PivotMode.Center)
            {
                pivotPoint = InternalEditorUtility.CalculateSelectionBounds(usePivotOnlyForParticles: true, onlyUseActiveSelection: false).center;
            }
            else
            {
                pivotPoint = activeTransform.position;
            }

            Vector3 move = position - pivotPoint;
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].position += move;
            }
        }
        public static void MoveOffsetTransforms(Transform[] transforms, Vector3 offset)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].position += offset;
            }
        }

        public static void Rotate(Transform[] transforms, Vector3 normal, float angle)
        {
            var rotation = Quaternion.AngleAxis(angle, normal);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].rotation *= rotation;
            }
        }
        public static void Rotate(Transform[] transforms, float angle)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                var rotation = Quaternion.AngleAxis(angle, transforms[i].up);
                transforms[i].rotation *= rotation;
            }
        }
    }
}
#endif 