using UnityEngine;

public class MotoSound : MonoBehaviour
{
    public AudioSource moteurAudio;
    public float pitchMin = 0.8f;
    public float pitchMax = 2.0f;
    public float vitesseMax = 20f;
    public Rigidbody motoRigidbody;

    void Update()
    {
        float vitesseActuelle = motoRigidbody.velocity.magnitude;
        moteurAudio.pitch = Mathf.Lerp(pitchMin, pitchMax, vitesseActuelle / vitesseMax);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
}
