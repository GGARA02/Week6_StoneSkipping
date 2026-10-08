using System.Collections.Generic;
using UnityEngine;

public class TrailRendererController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<TrailRenderer> SideTrailRenderers = new List<TrailRenderer>();
    public TrailRenderer leftTrailRenderer;
    public TrailRenderer rightTrailRenderer;
    public TrailRenderer forwardTrailRenderer;
    public TrailRenderer backwardTrailRenderer;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ActivateLightAttackTrailRenderer()
    {
        leftTrailRenderer.emitting = true;
        rightTrailRenderer.emitting = true;
    }
    public void DeactivateLightAttackTrailRenderer()
    {
        leftTrailRenderer.emitting = false;
        rightTrailRenderer.emitting = false;
    }

    public void ActivateHeavyAttackTrailRenderer()
    {
        forwardTrailRenderer.emitting = true;
        backwardTrailRenderer.emitting = true;
    }
    public void DeactivateHeavyAttackTrailRenderer()
    {
        forwardTrailRenderer.emitting = false;
        backwardTrailRenderer.emitting = false;
    }
    public void ActivateDashTrailRenderer()
    {
        backwardTrailRenderer.emitting = true;
    }
    public void DeactivateDashTrailRenderer()
    {
        backwardTrailRenderer.emitting = false;
    }


}
