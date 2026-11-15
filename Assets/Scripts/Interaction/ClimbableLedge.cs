using UnityEngine;

public class ClimbableLedge : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    public Vector3 LandingPosition
    {
        get
        {
            Vector3 worldUp = Physics.gravity.sqrMagnitude > 0.001f
                ? -Physics.gravity.normalized
                : Vector3.up;

            return GetLandingPosition(worldUp);
        }
    }

    public Vector3 GetLandingPosition(Vector3 upDirection)
    {
        Vector3 worldUp = upDirection.sqrMagnitude > 0.001f
            ? upDirection.normalized
            : Vector3.up;

        Transform highestPoint = null;
        float highestPosition = float.NegativeInfinity;

        if (points != null)
        {
            foreach (Transform point in points)
            {
                if (point == null)
                    continue;

                float position = Vector3.Dot(point.position, worldUp);

                if (position <= highestPosition)
                    continue;

                highestPosition = position;
                highestPoint = point;
            }
        }

        return highestPoint != null ? highestPoint.position : transform.position;
    }
}
