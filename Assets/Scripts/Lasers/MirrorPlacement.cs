using UnityEngine;

public class MirrorPlacement : MonoBehaviour
{
    [SerializeField] private Vector3 triangleFaceNormal = Vector3.forward;
    [SerializeField] private Vector3 firstSquareFaceNormal = Vector3.down;
    [SerializeField] private Vector3 secondSquareFaceNormal = Vector3.left;

    public Quaternion GetDropRotation(RaycastHit surfaceHit, Quaternion currentRotation)
    {
        LaserRedirectorCube cube = surfaceHit.collider.GetComponentInParent<LaserRedirectorCube>();

        if (cube != null)
            return GetCubeRotation(cube, currentRotation);

        return GetTriangleRotation(surfaceHit.normal, currentRotation);
    }

    public Vector3 GetSurfacePoint(RaycastHit surfaceHit)
    {
        LaserRedirectorCube cube = surfaceHit.collider.GetComponentInParent<LaserRedirectorCube>();
        return cube != null ? cube.OutputPosition : surfaceHit.point;
    }

    public Vector3 GetSurfaceNormal(RaycastHit surfaceHit)
    {
        LaserRedirectorCube cube = surfaceHit.collider.GetComponentInParent<LaserRedirectorCube>();
        return cube != null ? cube.OutputDirection.normalized : surfaceHit.normal.normalized;
    }

    private Quaternion GetTriangleRotation(Vector3 surfaceNormal, Quaternion currentRotation)
    {
        if (surfaceNormal.sqrMagnitude < 0.000001f ||
            triangleFaceNormal.sqrMagnitude < 0.000001f)
        {
            return currentRotation;
        }

        Vector3 targetNormal = -surfaceNormal.normalized;
        Vector3 firstNormal = currentRotation * triangleFaceNormal.normalized;
        Vector3 secondNormal = -firstNormal;
        Vector3 selectedNormal = Vector3.Dot(firstNormal, targetNormal) >= Vector3.Dot(secondNormal, targetNormal)
            ? firstNormal
            : secondNormal;

        return Quaternion.FromToRotation(selectedNormal, targetNormal) * currentRotation;
    }

    private Quaternion GetCubeRotation(LaserRedirectorCube cube, Quaternion currentRotation)
    {
        cube.GetTopFace(
            out _,
            out Vector3 surfaceNormal,
            out Vector3 cubeRight,
            out Vector3 cubeForward
        );

        Vector3 targetNormal = -surfaceNormal;
        Vector3 firstLocalNormal = firstSquareFaceNormal.sqrMagnitude > 0.000001f
            ? firstSquareFaceNormal.normalized
            : Vector3.down;
        Vector3 secondLocalNormal = secondSquareFaceNormal.sqrMagnitude > 0.000001f
            ? secondSquareFaceNormal.normalized
            : Vector3.left;
        Vector3 triangleNormal = triangleFaceNormal.sqrMagnitude > 0.000001f
            ? triangleFaceNormal.normalized
            : Vector3.forward;
        Vector3 firstNormal = currentRotation * firstLocalNormal;
        Vector3 secondNormal = currentRotation * secondLocalNormal;

        Vector3 selectedNormal = Vector3.Dot(firstNormal, targetNormal) >= Vector3.Dot(secondNormal, targetNormal)
            ? firstNormal
            : secondNormal;

        Quaternion rotation = Quaternion.FromToRotation(selectedNormal, targetNormal) * currentRotation;
        Vector3 mirrorAxis = Vector3.ProjectOnPlane(
            rotation * triangleNormal,
            surfaceNormal
        ).normalized;

        if (mirrorAxis.sqrMagnitude < 0.000001f)
            return rotation;

        Vector3 targetAxis = Mathf.Abs(Vector3.Dot(mirrorAxis, cubeRight)) >=
                             Mathf.Abs(Vector3.Dot(mirrorAxis, cubeForward))
            ? cubeRight
            : cubeForward;

        if (Vector3.Dot(mirrorAxis, targetAxis) < 0f)
            targetAxis = -targetAxis;

        float angle = Vector3.SignedAngle(mirrorAxis, targetAxis, surfaceNormal);
        return Quaternion.AngleAxis(angle, surfaceNormal) * rotation;
    }
}
