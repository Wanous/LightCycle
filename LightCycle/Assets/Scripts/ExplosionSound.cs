using UnityEngine;

public class ExplosionSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip explosionClip;

    public void JouerExplosion()
    {
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(explosionClip);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ennemi"))
        {
            // déclencher explosion et son
            GetComponent<ExplosionSound>().JouerExplosion();
        }
    }

}

