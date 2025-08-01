using UnityEngine;

public class FootstepSensor : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("발소리 클립들")]
    public AudioClip[] woodClips;
    public AudioClip[] metalClips;
    public AudioClip[] grassClips;
    public AudioClip[] defaultClips;

    [Header("설정")]
    public float stepInterval = 0.5f;         // 발소리 간격
    public float movementThreshold = 0.05f;   // 정지 판단 기준
    public float raycastDistance = 1.5f;      // 지면 감지 거리
    public bool debugRay = true;

    private Vector3 lastPosition;
    private float stepTimer = 0f;

    void Start()
    {
        lastPosition = transform.position;
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

  void Update()
{
    float distanceMoved = Vector3.Distance(transform.position, lastPosition);
    bool isMoving = distanceMoved > movementThreshold;

    if (isMoving)
    {
        if (!audioSource.isPlaying)
        {
            PlayLoopingFootstep();
        }
    }
    else
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    lastPosition = transform.position;
}

void PlayLoopingFootstep()
{
    if (defaultClips == null || defaultClips.Length == 0) return;

    int index = Random.Range(0, defaultClips.Length);
    audioSource.clip = defaultClips[index];
    audioSource.loop = true;
    audioSource.pitch = 1f; // 필요 시 조절
    audioSource.Play();
}

    // void PlayFootstep()
    // {
    //     Vector3 origin = transform.position + Vector3.up * 0.1f;
    //     Vector3 direction = Vector3.down;

    //     if (debugRay)
    //         Debug.DrawRay(origin, direction * raycastDistance, Color.red, 0.2f);

    //     if (Physics.Raycast(origin, direction, out RaycastHit hit, raycastDistance))
    //     {
    //         var mat = hit.collider.sharedMaterial;
    //         if (mat != null)
    //         {
    //             switch (mat.name)
    //             {
    //                 case "Wood": PlayRandom(woodClips); return;
    //                 case "Metal": PlayRandom(metalClips); return;
    //                 case "Grass": PlayRandom(grassClips); return;
    //             }
    //         }
    //     }

    //     PlayRandom(defaultClips);
    // }

    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clips[index]);
    }
}
