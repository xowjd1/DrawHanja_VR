using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioParticle : MonoBehaviour
{
    [Header("Vibration Settings")]
    // [SerializeField] private HapticImpulsePlayer rightControll;
    // [SerializeField] private HapticImpulsePlayer leftControll;
    [Range(0, 1)][SerializeField] private float intensity = 0.5f;
    [Range(0, 1)][SerializeField] private float duration = 0.2f;

    [Header("Audio Settings")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float volum = 1.0f;
    [SerializeField] private AudioClip clip;
    private AudioSource source;

    [Header("Play Particle Effects")]
    [SerializeField] private List<ParticleSystem> playParticleList = new List<ParticleSystem>();

    [Header("Destroy Particle Effects")]
    [SerializeField] private List<ParticleSystem> destoryParticleList = new List<ParticleSystem>();

    [Header("Particle Settings")]
    [SerializeField] private bool useRendererCenter = true;

    // 델리게이트
    public delegate void VisualFunction();
    public VisualFunction Visual;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    // public void PlayLeftVibration()
    // {
    //     leftControll.SendHapticImpulse(intensity, duration);
    // }
    //
    // public void PlayRightVibration()
    // {
    //     rightControll.SendHapticImpulse(intensity, duration);
    // }

    public void PlayAudio()
    {
        ControllPitch();
        source.PlayOneShot(clip, volum);
    }

    private void ControllPitch()
    {
        if (randomizePitch)
        {
            source.pitch = Random.Range(minPitch, maxPitch);
        }
    }

 public void PlayParticle()
{
    Vector3 position;

    if (useRendererCenter)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            position = renderer.bounds.center;
        else
        {
            Debug.LogWarning($"{gameObject.name}에 Renderer가 없어서 pivot 위치 사용");
            position = transform.position;
        }
    }
    else
    {
        position = transform.position;
    }

    foreach (var vfx in playParticleList)
    {
        if (vfx == null) continue;

        // ✅ 자식으로 붙임
        ParticleSystem instance = Instantiate(vfx, position, Quaternion.identity, transform);
        instance.Play();
    }
}


    public void DestroyParticle()
    {
        foreach (var vfx in destoryParticleList)
        {
            if (vfx == null) continue;

            Destroy(vfx.gameObject);
        }
    }
}
