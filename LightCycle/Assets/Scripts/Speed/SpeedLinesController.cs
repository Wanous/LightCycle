using UnityEngine;

public class SpeedLinesController : MonoBehaviour
{
    public Transform motoTransform;
    public ParticleSystem speedLines;
    public float speedThreshold = 20f;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = motoTransform.position;
    }

    void Update()
    {
        float distance = Vector3.Distance(motoTransform.position, lastPosition);
        float speed = distance / Time.deltaTime;

        if (speed > speedThreshold && !speedLines.isPlaying)
        {
            speedLines.Play();
        }
        else if (speed <= speedThreshold && speedLines.isPlaying)
        {
            speedLines.Stop();
        }

        lastPosition = motoTransform.position;
    }
}