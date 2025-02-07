using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailAxisInverter : MonoBehaviour
{
    private TrailRenderer trailRenderer;

    void Start()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            Debug.LogError("Trail Renderer not found on this object!");
        }
    }

    void LateUpdate()
    {
        if (trailRenderer != null)
        {
            int positionCount = trailRenderer.positionCount;
            Vector3[] positions = new Vector3[positionCount];
            trailRenderer.GetPositions(positions);

            // Inverse les axes Y et Z
            for (int i = 0; i < positionCount; i++)
            {
                positions[i] = new Vector3(positions[i].x, positions[i].z, positions[i].y);
            }

            trailRenderer.SetPositions(positions);
        }
    }
}
