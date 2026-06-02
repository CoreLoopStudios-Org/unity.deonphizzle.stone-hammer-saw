using UnityEngine;

public class HitDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Debug 1: Check if the physics engine registers ANY trigger enter event
        Debug.Log($"[HitDetector] Trigger entered by: {other.gameObject.name} (Tag: {other.gameObject.tag})");

        // Check if the hit object or its parent is tagged 'Victim'
        if (other.CompareTag("Victim") || (other.transform.parent != null && other.transform.parent.CompareTag("Victim")))
        {
            Debug.Log("[HitDetector] Victim tag check passed! Resolving Animator...");

            // Use GetComponentInParent to find the Animator on the root of the character
            Animator victimAnim = other.GetComponentInParent<Animator>();
            
            if (victimAnim != null)
            {
                Debug.Log("[HitDetector] Animator found! Firing 'FallDown' trigger.");
                victimAnim.SetTrigger("FallDown");
            }
            else
            {
                Debug.LogError("[HitDetector] ERROR: No Animator component found on the Victim or its parents!");
            }
        }
    }
}