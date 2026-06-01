using UnityEngine;

namespace Game.Presentation.Combat.Cards
{
    public readonly struct HandPose
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }

        public HandPose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
    }

    public sealed class ArcHandLayout : MonoBehaviour
    {
        public Transform Anchor;
        public float Radius = 3f;
        public float ArcAngle = 60f;
        public float CardWidth = 1.4f;
        public float YOffset = 0f;

        public HandPose GetPose(int index, int count)
        {
            Transform anchor = Anchor != null ? Anchor : transform;

            float angleDeg = 0f;
            if (count > 1)
            {
                float t = (float)index / (count - 1);
                angleDeg = Mathf.Lerp(-ArcAngle * 0.5f, ArcAngle * 0.5f, t);
            }

            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(
                Radius * Mathf.Sin(angleRad),
                Radius * Mathf.Cos(angleRad) + YOffset,
                0f
            );

            Quaternion localRot = Quaternion.Euler(0f, 0f, -angleDeg);

            Vector3 worldPos = anchor.position + anchor.rotation * localPos;
            Quaternion worldRot = anchor.rotation * localRot;

            return new HandPose(worldPos, worldRot, Vector3.one);
        }
    }
}
