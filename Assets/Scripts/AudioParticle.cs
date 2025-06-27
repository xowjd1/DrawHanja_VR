using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

[RequireComponent(typeof(AudioSource))]
public class AudioParticle : MonoBehaviour
{
    [Header("Vibration Settings")]
    [SerializeField] private HapticImpulsePlayer rightControll;
    [SerializeField] private HapticImpulsePlayer leftControll;
    [Range(0, 1)][SerializeField] private float intensity = 0.5f;
    [Range(0, 1)][SerializeField] private float duration = 0.2f;

    [Header("Audio Settings")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float volum = 1.0f;
    [SerializeField] private AudioClip clip;
    private AudioSource source;

    [Header("Particle Effects")]
    [SerializeField] private VisualEffect Demo1;
    [SerializeField] private VisualEffect Demo2;


    // 델리게이트
    public delegate void VisualFunction();
    public VisualFunction Visual;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void PlayLeftVibration()
    {
        leftControll.SendHapticImpulse(intensity, duration);
    }

    public void PlayRightVibration()
    {
        rightControll.SendHapticImpulse(intensity, duration);
    }

    public void PlayAudio()
    {
        source.PlayOneShot(clip, volum);
    }

    public void ControllPitch()
    {
        if (randomizePitch)
        {
            source.pitch = Random.Range(minPitch, maxPitch);
        }
    }

    public void PlayWaveParticle()
    {
        Demo1.Play();
        Demo2.Play();
    }
}
