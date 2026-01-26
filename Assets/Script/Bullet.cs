using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int bulletDamage = 25;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Zombie"))
        {
            if (collision.gameObject.GetComponent<Zombie>().isDead == false)
            {
                collision.gameObject.GetComponent<Zombie>().TakeDamage(bulletDamage);

            }
            CreateBloodSprayEffect(collision);
            Destroy(gameObject);
        }
        else
        {
            CreateBulletImpactEffect(collision);
            Destroy(gameObject);
        }
    }

    private void CreateBulletImpactEffect(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
        hole.transform.SetParent(collision.gameObject.transform);
    }

    private void CreateBloodSprayEffect(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        GameObject bloodSprayPrefab = Instantiate(
            GlobalReferences.Instance.bloodSprayEffect,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
        bloodSprayPrefab.transform.SetParent(collision.gameObject.transform);
    }
}