using UnityEngine;

public class PickUpWallet : MonoBehaviour
{
    [SerializeField] private int moneyAmount = 50;
    [SerializeField] private bool destroyOnPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player collided with the wallet
        if (other.CompareTag("Player"))
        {
            // Add money to the player's progression
            SimpleProgression progression = SimpleProgression.Instance;
            if (progression != null)
            {
                progression.AddMoney(moneyAmount);
                

                // Destroy the wallet if configured to do so
                if (destroyOnPickup)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                
            }
        }
    }
}
