using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaserReflectiveSurface : MonoBehaviour
{
    [SerializeField] private LaserMirror mirror;
    [SerializeField] private Vector3 localNormal = Vector3.up;

    public LaserMirror Mirror => mirror;
    public Vector3 SurfaceNormal
    {
        get
        {
            Vector3 normal = transform.TransformDirection(localNormal);
            return normal.sqrMagnitude > 0.000001f ? normal.normalized : transform.up;
        }
    }

    private void Awake()
    {
        if (mirror == null)
            mirror = GetComponentInParent<LaserMirror>();
    }
}
