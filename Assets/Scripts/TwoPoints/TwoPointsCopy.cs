using UnityEngine;

public class TwoPointsCopy : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<CharacterAnimationController>() == null)
            gameObject.AddComponent<CharacterAnimationController>();
    }
}
