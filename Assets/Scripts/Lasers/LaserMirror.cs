using UnityEngine;

public class LaserMirror : MonoBehaviour
{
    [SerializeField] private Transform splitPoint;
    [SerializeField] private LaserReflectiveSurface reflectiveSurface;

    public Vector3 SplitPosition => splitPoint != null ? splitPoint.position : transform.position;
    public Vector3 ReflectionNormal => reflectiveSurface != null
        ? reflectiveSurface.SurfaceNormal
        : transform.up;

    private void Awake()
    {
        if (reflectiveSurface == null)
            reflectiveSurface = GetComponentInChildren<LaserReflectiveSurface>();
    }

    public Vector3 GetSplitDirection(Vector3 incomingDirection)
    {
        return Vector3.Reflect(incomingDirection.normalized, ReflectionNormal).normalized;
    }
}
