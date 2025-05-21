using UnityEngine;

public class MotoSound : MonoBehaviour
{
   /* public AudioSource moteurAudio;
    public float pitchMin = 0.8f;
    public float pitchMax = 2.0f;
    public float vitesseMax = 20f;
    public Rigidbody motoRigidbody;

    void Update()
    {
        float vitesseActuelle = motoRigidbody.linearVelocity.magnitude;
        moteurAudio.pitch = Mathf.Lerp(pitchMin, pitchMax, vitesseActuelle / vitesseMax);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }*/
       public AudioSource audioSource;
       public AudioClip idleSound;
       public AudioClip accelerationSound;

       void Start()
       {
           audioSource.clip = idleSound;
           audioSource.loop = true;
           audioSource.Play();
       }

       void Update()
       {
           if (Input.GetKey(KeyCode.Z))
           {
               // Si le son n'est pas déjà celui d'accélération
               if (audioSource.clip != accelerationSound)
               {
                   audioSource.clip = accelerationSound;
                   audioSource.Play();
               }
           }
           else
           {
               // Si le son n'est pas déjà celui de ralenti
               if (audioSource.clip != idleSound)
               {
                   audioSource.clip = idleSound;
                   audioSource.Play();
               }
           }
       }
    
}
