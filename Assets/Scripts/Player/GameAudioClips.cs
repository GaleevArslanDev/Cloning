using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameAudioClips : MonoBehaviour
{
    private static GameAudioClips instance;

    [Header("Player")]
    [SerializeField] private AudioClip[] footsteps;
    [SerializeField] private AudioClip falling;
    [SerializeField] private AudioClip climbing;
    [SerializeField] private AudioClip pickup;
    [SerializeField] private AudioClip drop;
    [SerializeField] private AudioClip water;
    [SerializeField] private AudioClip respawn;

    [Header("Interactions")]
    [SerializeField] private AudioClip platePressed;
    [SerializeField] private AudioClip plateReleased;
    [SerializeField] private AudioClip buttonPressed;
    [SerializeField] private AudioClip buttonReleased;

    [Header("Laser")]
    [SerializeField] private AudioClip laserLoop;
    [SerializeField] private AudioClip laserReceived;

    [Header("Environment")]
    [SerializeField] private AudioClip floorMovement;
    [SerializeField] private AudioClip doorOpening;
    [SerializeField] private AudioClip flaskClosing;
    [SerializeField] private AudioClip flaskOpening;
    [SerializeField] private AudioClip rebuilding;

    public static GameAudioClips Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GameAudioClips>();

            return instance;
        }
    }

    public AudioClip[] Footsteps => footsteps;
    public AudioClip Falling => falling;
    public AudioClip Climbing => climbing;
    public AudioClip Pickup => pickup;
    public AudioClip Drop => drop;
    public AudioClip Water => water;
    public AudioClip Respawn => respawn;
    public AudioClip PlatePressed => platePressed;
    public AudioClip PlateReleased => plateReleased;
    public AudioClip ButtonPressed => buttonPressed;
    public AudioClip ButtonReleased => buttonReleased;
    public AudioClip LaserLoop => laserLoop;
    public AudioClip LaserReceived => laserReceived;
    public AudioClip FloorMovement => floorMovement;
    public AudioClip DoorOpening => doorOpening;
    public AudioClip FlaskClosing => flaskClosing;
    public AudioClip FlaskOpening => flaskOpening;
    public AudioClip Rebuilding => rebuilding;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
