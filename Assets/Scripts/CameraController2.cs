using UnityEngine;

public class CameraController2 : MonoBehaviour
{
    public GameObject focalPoint;
    public float distance = 5f;
    public float verticalSensitivity = 0.15f;
    public float horizantalSensitiviy = 0.15f;
    public float minPitch = -30.0f;
    public float maxPitch = 60.0f;

    private float currentHZRotation = 0;
    private float currentVTRotation = 0;

    private InputSystem_Actions _inputActions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }


    void LateUpdate()
    {
        Vector2 look = _inputActions.Player.Look.ReadValue<Vector2>();

        currentHZRotation += look.x * horizantalSensitiviy;
        currentVTRotation -= look.y * verticalSensitivity;
        currentVTRotation = Mathf.Clamp(currentVTRotation, minPitch, maxPitch);
        Quaternion rot = Quaternion.Euler(currentVTRotation, currentHZRotation, 0);
        transform.rotation = rot;

        transform.position = focalPoint.transform.position + rot * Vector3.back * distance;
    }

    public void GetInputAction(InputSystem_Actions action)
    {
        _inputActions = action;
    }
}
