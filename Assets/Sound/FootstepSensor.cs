using UnityEngine;

public class FootstepSensor : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("기본 발소리")]
    public AudioClip defaultClip;

    [Header("재질 이름 (materialNames[i] 에 대응되는 소리 = footstepClips[i])")]
    public string[] materialNames;
    public AudioClip[] footstepClips;

    [Header("설정")]
    public float stepInterval = 0.5f;
    public float movementThreshold = 0.05f;
    public float raycastDistance = 1.5f;
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
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 direction = Vector3.down;

        if (debugRay)
            Debug.DrawRay(origin, direction * raycastDistance, Color.red, 0.2f);

        AudioClip clipToUse = defaultClip;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, raycastDistance))
        {
            var mat = hit.collider.sharedMaterial;

            if (mat != null)
            {
                for (int i = 0; i < materialNames.Length && i < footstepClips.Length; i++)
                {
                    if (mat.name == materialNames[i])
                    {
                        clipToUse = footstepClips[i];
                        break;
                    }
                }
            }
        }

        if (clipToUse == null) return;

        audioSource.clip = clipToUse;
        audioSource.loop = true;
        audioSource.pitch = 1f;
        audioSource.Play();
    }
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

// void PlayRandom(AudioClip[] clips)
// {
//     if (clips == null || clips.Length == 0) return;

//     int index = Random.Range(0, clips.Length);
//     audioSource.pitch = Random.Range(0.95f, 1.05f);
//     audioSource.PlayOneShot(clips[index]);
// }

