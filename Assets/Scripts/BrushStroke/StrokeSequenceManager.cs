using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class StrokeSequenceManager : MonoBehaviour
{
    [Header("순서대로 등록 (1 -> 4)")]
    public List<StrokeSequence> sequences;

    [Header("오브젝트 날아가는 속도")]
    public float flySpeed = 5f;
    public int flyDelay = 3;

    private int currentIndex = 0;
    private bool isFlying = false;

    [Header("사라지게 만들 파티클")]
    public ParticleSystem targetParticle;
    public float fadeDuration = 2f;
    public bool useFadeOut = true;


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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isFlying)
        {
            FlyAllObjectsSequentially().Forget();
        }

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

    private void DeactivateChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
        }
    }
    private async UniTaskVoid FlyAllObjectsSequentially()
    {
        isFlying = true;

        foreach (var seq in sequences)
        {
            Rigidbody rb = seq.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = seq.transform.forward * flySpeed;
            }
            else
            {
                // Rigidbody가 없으면 추가
                rb = seq.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.linearVelocity = seq.transform.forward * flySpeed;
            }

            await UniTask.Delay(flyDelay * 1000);
        }

        isFlying = false;
    }


    public async UniTaskVoid FadeOut()
    {
        if (targetParticle == null) return;

        ParticleSystem[] particles = targetParticle.GetComponentsInChildren<ParticleSystem>();

        List<(Material mat, Color originalColor)> materials = new();

        foreach (var ps in particles)
        {
            ps.loop = false;

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


}
