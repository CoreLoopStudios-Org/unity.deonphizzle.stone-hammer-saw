using UnityEngine;

public class HitDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // যার সাথে ধাক্কা লাগবে, তার গায়ে 'Victim' ট্যাগ আছে কি না চেক করবে
        if (other.CompareTag("Victim"))
        {
            // ভিকটিমের গায়ের Animator কম্পোনেন্টটি খুঁজে বের করবে
            Animator victimAnim = other.GetComponent<Animator>();
            
            if (victimAnim != null)
            {
                // ভিকটিমকে পড়ে যাওয়ার ট্রিগারটি ফায়ার করবে
                victimAnim.SetTrigger("FallDown");
            }
        }
    }
}