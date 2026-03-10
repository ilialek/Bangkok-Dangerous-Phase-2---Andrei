using UnityEngine;

public class PickUpHealthItem : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 1f;        // Speed of floating motion
    [SerializeField] private float floatHeight = 0.5f;     // Height of floating motion
    [SerializeField] private float spinSpeedX = 45f;       // Rotation speed around X axis
    [SerializeField] private float spinSpeedY = 90f;       // Rotation speed around Y axis
    [SerializeField] private float spinSpeedZ = 30f;       // Rotation speed around Z axis

    private Vector3 startPosition;
    private float floatTimer;

    void Start()
    {
        startPosition = transform.position;
        floatTimer = 0f;
    }

    void Update()
    {
        // Handle floating motion
        floatTimer += Time.deltaTime;
        float newY = startPosition.y + (Mathf.Sin(floatTimer * floatSpeed) * floatHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Handle spinning on multiple axes
        transform.Rotate(new Vector3(spinSpeedX, spinSpeedY, spinSpeedZ) * Time.deltaTime);
    }
}
