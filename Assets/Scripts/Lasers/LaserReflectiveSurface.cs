using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaserReflectiveSurface : MonoBehaviour
{
    [SerializeField] private LaserMirror mirror;
    [SerializeField] private Vector3 localNormal = Vector3.up;

    public LaserMirror Mirror => mirror;
    public Vector3 SurfaceNormal => transform.TransformDirection(localNormal).normalized;

    private void Awake()
    {
        if (mirror == null)
            mirror = GetComponentInParent<LaserMirror>();
    }
}
