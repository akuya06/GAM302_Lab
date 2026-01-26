using UnityEngine;
using System.Collections;

public class PlayerStun : MonoBehaviour
{
    private CharacterController characterController;
    private bool isStunned = false;
    private float stunTimer = 0f;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
            }
        }
    }

    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
    }

    public bool IsStunned()
    {
        return isStunned;
    }
}
