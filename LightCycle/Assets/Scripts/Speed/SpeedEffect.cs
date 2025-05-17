using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SpeedEffect : MonoBehaviour
{
    public Transform motoTransform;
    public PostProcessVolume postVolume;
    private MotionBlur blur;

    public float speedThreshold = 30f;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = motoTransform.position;
        postVolume.profile.TryGetSettings(out blur);
    }

    void Update()
    {
        float distance = Vector3.Distance(motoTransform.position, lastPosition);
        float speed = distance / Time.deltaTime;

        blur.enabled.value = speed > speedThreshold;
        blur.shutterAngle.value = Mathf.Lerp(0f, 270f, (speed - speedThreshold) / 50f);

        lastPosition = motoTransform.position;
    }
}