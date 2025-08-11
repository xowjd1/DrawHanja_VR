using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class StrokeSequenceManager : MonoBehaviour
{
    public List<StrokeSequence> sequences;

    [Header("날아가는 오브젝트")]
    public float flySpeed = 5f;
    public float flyDelay = 3f;
    public float flyLifetime = 5f;

    [Header("시퀀스 전부 파괴 후 본체 제거까지 대기 시간")]
    public float selfDestructDelay = 3f;
    private int currentIndex = 0;
    private bool isFlying = false;

    [Header("사라지게 만들 파티클")]
    public ParticleSystem targetParticle;
    public float fadeDuration = 2f;
    public bool useFadeOut = true;

    [Header("공격 파티클")]
    public float particleDistanceFromCamera = 2f;
    public ParticleSystem attacknParticle;

    public Vector3 addRotation = new Vector3(0, 0, 0);


    void Start()
    {
        foreach (var seq in sequences)
        {
            seq.manager = this;
            seq.DeactivateChildren(); // 리스트 기준으로만 비활성화
        }

        if (sequences.Count > 0)
        {
            ActivateSequence(0);
        }
        
        FlyAllObjectsSequentially().Forget();
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space) && !isFlying)
        // {
        //     FlyAllObjectsSequentially().Forget();
        // }

    }

    public void ActivateNextSequence()
    {
        currentIndex++;

        if (currentIndex < sequences.Count)
        {
            ActivateSequence(currentIndex);
        }
        else
        {
            OnAllSequencesComplete().Forget();
        }
    }

    private void ActivateSequence(int index)
    {
        var sequence = sequences[index];
        sequence.ResetSequence(); // 첫 포인트만 활성화하도록 초기화
    }

    public async UniTaskVoid OnAllSequencesComplete()
    {
        Debug.Log("모든 StrokeSequence 완료!");

        await UniTask.Delay(500);
        PlaySpawnEffect();// 모든 시퀀스 완료시 공격 파티클 함수 실행

        if (useFadeOut && targetParticle != null)
        {
            FadeOut().Forget();
            await UniTask.Delay(3000);
        }
        else
        {
            await UniTask.Delay(3000);
        }
        Destroy(gameObject);
    }

    // private void DeactivateChildren(Transform parent)
    // {
    //     foreach (Transform child in parent)
    //     {
    //         child.gameObject.SetActive(false);
    //     }
    // }
    private async UniTaskVoid FlyAllObjectsSequentially()
    {
        isFlying = true;

        Vector3 flyDirection = transform.forward; // 매니저 기준 앞 방향

        foreach (var seq in sequences)
        {
            Rigidbody rb = seq.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = seq.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
            }

            rb.linearVelocity = flyDirection * flySpeed;

            // 오브젝트를 flyLifetime 초 후에 파괴
            Destroy(seq.gameObject, flyLifetime);

            await UniTask.Delay((int)(flyDelay * 1000f));
        }

        isFlying = false;

        CheckAllSequencesDestroyedAndSelfDestruct().Forget();

    }

    public async UniTaskVoid FadeOut()
    {
        if (targetParticle == null) return;

        ParticleSystem[] particles = targetParticle.GetComponentsInChildren<ParticleSystem>();

        List<(Material mat, Color originalColor)> materials = new();

        foreach (var ps in particles)
        {
            var main = ps.main;
            main.loop = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var mat = renderer.material;
                materials.Add((mat, mat.color));
            }
        }


        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            foreach (var (mat, originalColor) in materials)
            {
                Color faded = originalColor;
                faded.a = Mathf.Lerp(originalColor.a, 0f, t);
                mat.color = faded;
            }
            await UniTask.Yield();
        }

        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        bool AnyAlive()
        {
            foreach (var ps in particles)
                if (ps.IsAlive()) return true;
            return false;
        }

        while (AnyAlive())
        {
            await UniTask.Yield();
        }

        Destroy(targetParticle.gameObject);
    }

    private async UniTaskVoid CheckAllSequencesDestroyedAndSelfDestruct()
    {
        // 모든 시퀀스가 파괴될 때까지 대기
        while (true)
        {
            bool anyAlive = false;
            foreach (var seq in sequences)
            {
                if (seq != null) // 아직 살아있는 시퀀스가 있다면
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
                break;

            await UniTask.Yield();
        }

        // 설정된 시간만큼 대기 후 본체 삭제
        await UniTask.Delay((int)(selfDestructDelay * 1000f));
        Destroy(gameObject);
    }

    public void PlaySpawnEffect()
    {
        Camera cam = Camera.main;
        if (cam == null || attacknParticle == null)
        {
            Debug.LogWarning("메인 카메라 또는 파티클 프리팹이 없습니다.");
            return;
        }

        // 카메라 앞 위치 계산
        Vector3 pos = cam.transform.position + cam.transform.forward * particleDistanceFromCamera;
        Quaternion rot = cam.transform.rotation * Quaternion.Euler(addRotation);

        ParticleSystem ps = Instantiate(attacknParticle, pos, rot);
        ps.Play();

        // Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

}
