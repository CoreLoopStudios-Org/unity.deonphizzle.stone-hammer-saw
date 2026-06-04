using UnityEngine;

public class SquidGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float timeLimit = 100f; 
    public AudioSource dollMusic;  
    public float reactionTime = 0.5f; // থামার জন্য প্লেয়ারকে ০.৫ সেকেন্ড সময় দেওয়া হলো
    
    [Header("Status (Don't Touch)")]
    public bool isGreenLight = true;
    public bool isGameOver = false;

    private float lightTimer = 0f;
    private float currentReactionTime = 0f; // রিঅ্যাকশন টাইমের হিসাব রাখার জন্য

    void Start()
    {
        SwitchToGreenLight();
    }

    void Update()
    {
        if (isGameOver) return;

        // ১. ১০০ সেকেন্ডের টাইমার কমানো
        timeLimit -= Time.deltaTime;
        if (timeLimit <= 0)
        {
            EliminatePlayer("Time is up! You failed to reach the chest.");
        }

        // ২. র্যান্ডম সময়ে গান থামানো এবং চালু করা
        lightTimer -= Time.deltaTime;
        if (lightTimer <= 0)
        {
            if (isGreenLight) SwitchToRedLight();
            else SwitchToGreenLight();
        }

        // ৩. গান বন্ধ থাকা অবস্থায় মুভমেন্ট চেক করা
        if (!isGreenLight)
        {
            // গান থামার পর প্লেয়ারকে থামার জন্য একটু সময় দেওয়া
            if (currentReactionTime > 0)
            {
                currentReactionTime -= Time.deltaTime;
            }
            else
            {
                // সময় শেষ হওয়ার পর যদি মুভমেন্ট ধরা পড়ে
                float moveInput = SimpleMobileJoystick.InputDirection.magnitude;
                bool pcMovement = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;

                if (moveInput > 0.1f || pcMovement)
                {
                    EliminatePlayer("You moved during RED LIGHT! Eliminated.");
                }
            }
        }
    }

    void SwitchToGreenLight()
    {
        isGreenLight = true;
        dollMusic.Play(); // Pause থেকে Play করলে যেখান থেকে থেমেছিল সেখান থেকেই বাজবে
        lightTimer = Random.Range(3f, 6f); 
    }

    void SwitchToRedLight()
    {
        isGreenLight = false;
        dollMusic.Pause(); // Stop-এর বদলে Pause ব্যবহার করা হলো
        currentReactionTime = reactionTime; // থামার সময় সেট করা হলো
        lightTimer = Random.Range(2f, 3.5f); 
    }

    public void EliminatePlayer(string reason)
    {
        isGameOver = true;
        dollMusic.Pause();
        Debug.Log("<color=red>GAME OVER: " + reason + "</color>");
    }

    public void PlayerWon()
    {
        isGameOver = true;
        dollMusic.Pause();
        Debug.Log("<color=green>YOU WON! You reached the chest in time.</color>");
    }
}