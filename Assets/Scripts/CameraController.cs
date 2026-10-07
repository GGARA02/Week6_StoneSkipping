using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private float smoothTime;

    [SerializeField]
    private Vector3 offset;

    private bool isActive;
    private Vector3 currentCameraSpeed;
    // private float currentFov;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void LateUpdate()
    {
        Move();
    }

    public void Initialize()
    {
        cam = GetComponent<Camera>();
        currentCameraSpeed = Vector3.zero;
        //currentFov = 60;
        isActive = false;
    }

    private void Move()
    {
        Vector3 targetWorldPos = target.TransformPoint(offset);
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, targetWorldPos, ref currentCameraSpeed, smoothTime * (1 / Time.timeScale)); ;
        transform.position = newPosition;
        Vector3 lookTarget = target.transform.position;
        transform.LookAt(lookTarget);
        // cam.fieldOfView = currentFov;
    }
}
