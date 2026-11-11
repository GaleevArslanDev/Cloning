using UnityEngine;

public class ActorPressurePlate : PressurePlate
{
    protected override GameObject GetActivator(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player != null)
            return player.gameObject;

        ClonePlayback clone = other.GetComponentInParent<ClonePlayback>();

        if (clone != null)
            return clone.gameObject;

        return null;
    }
}
