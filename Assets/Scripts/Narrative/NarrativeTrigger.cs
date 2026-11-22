using UnityEngine;

public class NarrativeTrigger : MonoBehaviour
{
    [SerializeField] private NarrativePlayer player;
    [SerializeField] private NarrativeSequence sequence;

    private void Awake()
    {
        if (player == null)
            player = NarrativePlayer.Instance;

        if (player == null)
            player = FindFirstObjectByType<NarrativePlayer>();
    }

    public void Play()
    {
        FindPlayer();

        if (player != null)
            player.Play(sequence);
    }

    public void PlayQueued()
    {
        FindPlayer();

        if (player != null)
            player.PlayQueued(sequence);
    }

    public void Stop()
    {
        FindPlayer();

        if (player != null)
            player.Stop();
    }

    private void FindPlayer()
    {
        if (player == null)
            player = NarrativePlayer.Instance;

        if (player == null)
            player = FindFirstObjectByType<NarrativePlayer>();
    }
}
