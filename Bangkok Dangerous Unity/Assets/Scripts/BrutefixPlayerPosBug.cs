using UnityEngine;

public class BrutefixPlayerPosBug : MonoBehaviour
{
    private float timeSinceWPress = 0f;
    private bool wPressedThisFrame = false;
    private bool hasExecuted = false;

    void Start()
    {
        
    }

    void Update()
    {
        // Only execute once
        if (hasExecuted)
            return;

        // Check if W or forward input is pressed
        if (Input.GetKey(KeyCode.W) || Input.GetAxis("Vertical") > 0)
        {
            wPressedThisFrame = true;
            timeSinceWPress = 0f;
        }

        // If W was pressed, count up the timer
        if (wPressedThisFrame)
        {
            timeSinceWPress += Time.deltaTime;

            // After half a second, set player Y position to 0 and destroy script
            if (timeSinceWPress >= 0.5f)
            {
                Vector3 newPos = transform.position;
                newPos.y = 0f;
                transform.position = newPos;
                hasExecuted = true;
                Destroy(this);
            }
        }
    }
}
