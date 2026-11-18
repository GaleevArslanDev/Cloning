using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelFinishTrigger : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float centeringSpeed = 2.5f;
    [SerializeField] private float maximumCenteringTime = 2f;

    private bool activated;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();
        if (centerPoint == null)
            centerPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || levelManager == null)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        activated = true;
        levelManager.ReachFinish(
            player,
            centerPoint,
            centeringSpeed,
            maximumCenteringTime
        );
    }
}
