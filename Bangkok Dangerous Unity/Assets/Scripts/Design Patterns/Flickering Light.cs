using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    private Light lightToFlicker;

    [SerializeField, Range(0f, 140f)] private float minAt = 0f;
    [SerializeField, Range(0f, 140f)] private float maxAt = 140f;
    [SerializeField, Min(0f)] private float timeBetweenChange = 0.1f;

    private float currentTimer;

    private void Awake()
    {
        if (lightToFlicker == null)
        {
            lightToFlicker = GetComponent<Light>();
        }

        ValidateAtBounds();
    }

    private void Update()
    {
        currentTimer += Time.deltaTime;
        if (!(currentTimer >= timeBetweenChange)) return;

        lightToFlicker.range = Random.Range(minAt, maxAt);
        currentTimer = 0;
    }

    private void ValidateAtBounds()
    {
        if (!(minAt > maxAt))
        {
            return;
        }

        Debug.LogWarning($"Min AT is greater than max AT, swapping values!");
        (minAt, maxAt) = (maxAt, minAt);
    }
}

