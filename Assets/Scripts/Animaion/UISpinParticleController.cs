using UnityEngine;

public class UISpinParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem uiParticleSystem;

    public void PlayParticles()
    {
        if (uiParticleSystem != null)
        {
            var main = uiParticleSystem.main;
            main.loop = true;
            if (!uiParticleSystem.isPlaying)
            {
                uiParticleSystem.Play();
            }
        }
    }

    public void StopParticles()
    {
        if (uiParticleSystem != null)
        {
            var main = uiParticleSystem.main;
            main.loop = false;
            // Stop emitting new particles, allow remaining to fade out naturally
            uiParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
