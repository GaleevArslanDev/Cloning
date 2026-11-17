using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelFinishTrigger : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;

    private bool activated;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || levelManager == null)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        activated = true;
        levelManager.ReachFinish();
    }
}
