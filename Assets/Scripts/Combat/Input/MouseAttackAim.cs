using UnityEngine;

namespace AbyssBar.Combat
{
    public static class MouseAttackAim
    {
        public static bool TryProject(Camera camera, Vector2 pointer, Vector3 origin, Vector2 previousDirection,
            out Vector2 point, out Vector2 direction)
        {
            point = origin; direction = previousDirection;
            if (camera == null || !camera.isActiveAndEnabled || !Finite(pointer.x) || !Finite(pointer.y)
                || !Finite(origin.x) || !Finite(origin.y) || !Finite(origin.z) || !camera.pixelRect.Contains(pointer)) return false;
            Ray ray = camera.ScreenPointToRay(pointer);
            var plane = new Plane(Vector3.forward, origin);
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 world = ray.GetPoint(distance);
            if (!Finite(world.x) || !Finite(world.y)) return false;
            point = world; Vector2 offset = point - (Vector2)origin;
            direction = offset.sqrMagnitude > .0001f ? offset.normalized
                : previousDirection.sqrMagnitude > .0001f ? previousDirection.normalized : Vector2.right;
            return Finite(direction.x) && Finite(direction.y) && direction.sqrMagnitude > .0001f;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
