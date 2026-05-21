using UnityEngine;
using Photon.Pun;

public class PhoneSensor : MonoBehaviourPun
{
    private bool hasPickedUp = false;
    private float shakeThreshold = 2.5f; // টেবিল থেকে তোলার সংবেদনশীলতা

    void Update()
    {
        if (hasPickedUp) return;

        // টেবিল থেকে ফোন তোলার সময় গতির পরিবর্তন
        if (Input.acceleration.magnitude > shakeThreshold)
        {
            hasPickedUp = true;
            float pickTime = (float)PhotonNetwork.Time; // সার্ভারের সঠিক সময় রেকর্ড করা
            
            // মাস্টার ক্লায়েন্টকে জানানো কে কত সময় আগে ফোন তুলেছে
            FindAnyObjectByType<GameplayController>().photonView.RPC("RegisterPickupTime", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, pickTime);
        }
    }
}