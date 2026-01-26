using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
    [SerializeField] int health = 100;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float stunDuration = 1.5f;
    public int damage = 20;
    public bool isDead = false;
    private Animator animator;


    public Transform player; // Gán player từ Inspector
    private NavMeshAgent agent;
    private bool isAttacking = false;
    private bool wasRunning = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null || agent == null || animator == null) return;
        if (!agent.enabled || health <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        agent.SetDestination(player.position);
        var audio = SoundManager.Instance.zombieAudioSource;

        if (distanceToPlayer > attackRange)
        {
            animator.SetBool("isRunning", true);
            isAttacking = false;
            if (!wasRunning)
            {
                if (audio.clip != SoundManager.Instance.zombieChasing)
                {
                    audio.clip = SoundManager.Instance.zombieChasing;
                    audio.loop = true;
                    audio.Play();
                }
                wasRunning = true;
            }
        }
        else
        {
            animator.SetBool("isRunning", false);
            if (wasRunning)
            {
                if (audio.isPlaying && audio.clip == SoundManager.Instance.zombieChasing)
                    audio.Stop();
                wasRunning = false;
            }
            if (!isAttacking)
            {
                audio.PlayOneShot(SoundManager.Instance.zombieAttack);
                animator.SetTrigger("Attack");
                isAttacking = true;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        var audio = SoundManager.Instance.zombieAudioSource;
        if (audio.isPlaying && audio.clip == SoundManager.Instance.zombieChasing)
            audio.Stop(); // Tắt chasing khi bị hurt hoặc die
        if (health <= 0)
        {
            isDead = true;
            animator.SetBool("isDead", true);
            audio.PlayOneShot(SoundManager.Instance.zombieDie);
            Die();
        }
        else if (animator != null)
        {
            audio.PlayOneShot(SoundManager.Instance.zombieHurt);
            animator.SetTrigger("Hurt");
        }
    }

    private void Die()
    {
        SoundManager.Instance.zombieAudioSource.PlayOneShot(SoundManager.Instance.zombieDie);
        if (animator != null)
            animator.SetTrigger("Die");
        
        if (agent != null)
            agent.enabled = false;
            
        Destroy(gameObject, 3f);
    }

    public void ResetAttack()
    {
        isAttacking = false;
    }
}