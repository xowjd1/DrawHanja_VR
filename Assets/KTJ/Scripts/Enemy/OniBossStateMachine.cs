using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OniBossStateMachine : MonoBehaviour
{
    private OniBossState currentState;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;
    private bool phase2Triggered = false;

    [Header("Weapon Mounts")]
    [SerializeField] private Transform backMount;
    [SerializeField] private Transform handMount;
    [SerializeField] private GameObject weapon;

    [Header("References")]
    public Animator animator;
    [SerializeField] private GameObject player;
    [SerializeField] private SphereCollider leftHand;
    [SerializeField] private SphereCollider rightHand;
    [SerializeField] private BoxCollider bossWeapon;

    [SerializeField] private GameObject smashVFXPrefab;
    [SerializeField] private SphereCollider smashCollider;
    [SerializeField] private Transform smashVFXSpawnPoint;

    [Header("Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxIntro;
    public AudioClip sfxPhase2Start;
    public AudioClip sfxSmash;

    [Header("Scene Transition on Death")]
    public string nextSceneName = "NextScene";
    public float dieExtraDelay = 0.0f;          // (이제 사용 안 해도 됨)
    public float fadeDuration  = 0.8f;
    public CanvasGroup fadeCanvasFallback;      // VolumeFader 없을 때 폴백

    [Header("UI before Fade")]
    public float endingUiStartAfterDeath = 4f;
    public float fadeStartAfterDeath     = 6f;

    public GameObject endingUI;

    // 1페이즈
    public OniBossState CreateIntroState() => new BossIntroState(this);
    public OniBossState CreateMoveState() => new MoveToPlayerState(this);
    public OniBossState CreateAttack1State() => new Boss1NorAttackState(this);
    public OniBossState CreateAttack2State() => new Boss1NorAttack2State(this);
    // 2페이즈
    public OniBossState CreatePhase2Start() => new Boss2PhaseStartState(this);
    public OniBossState CreateMoveToPlayer2Phase() => new MoveToPlayer2PhaseState(this);
    public OniBossState CreateBoss2NorAttack() => new Boss2NorAttackState(this);
    public OniBossState CreateBoss2ComboAttack() => new Boss2ComboAttackState(this);
    public OniBossState CreateBoss2SmashAttack() => new Boss2SmashAttackState(this);
    public OniBossState CreateDieState() => new BossDieState(this);

    // Exposed
    public Transform PlayerTransform => player.transform;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float AttackRange => attackRange;
    public float DetectionRange => detectionRange;

    void Awake() => currentHealth = maxHealth;
    void Start() => ChangeState(CreateIntroState());
    void Update() => currentState?.Update();

    public void ChangeState(OniBossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void OnPhase2StartFinished() => ChangeState(CreateMoveToPlayer2Phase());

    public void OnAttackFinished()
    {
        if (currentState is Boss1NorAttackState || currentState is Boss1NorAttack2State)
        {
            DisableLeftAttack();
            DisableRightAttack();
            ChangeState(CreateMoveState());
        }
    }

    public void On2PhaseAttackFinished()
    {
        if (currentState is Boss2NorAttackState || currentState is Boss2ComboAttackState || currentState is Boss2SmashAttackState)
            ChangeState(CreateMoveToPlayer2Phase());
    }

    public void ApplyDamage(float dmg)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        if (!phase2Triggered && currentHealth <= maxHealth * 0.5f)
        {
            phase2Triggered = true;
            ChangeState(CreatePhase2Start());
        }

        if (currentHealth == 0)
            ChangeState(CreateDieState());
    }

    public void EquipWeaponToHand()
    {
        if (!weapon || !handMount) return;
        weapon.transform.SetParent(handMount, false);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.localScale = Vector3.one;
    }

    public void SmashVFX()
    {
        if (smashVFXPrefab != null && smashVFXSpawnPoint != null)
        {
            var vfxInstance = Instantiate(
                smashVFXPrefab,
                smashVFXSpawnPoint.position,
                Quaternion.Euler(0f, -90f, 0f)
            );

            EnableWeaponAttack();
            EnableSmashAttack();

            StartCoroutine(DisableAfter(vfxInstance, 1f));
        }
    }

    IEnumerator DisableAfter(GameObject vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfxInstance) Destroy(vfxInstance);
        DisableWeaponAttack();
        DisableSmashAttack();
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!clip) return;
        var src = audioSource ?? (audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>());
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.PlayOneShot(clip, volume);
    }

    public void Anim_PlaySmashSfx() => PlaySfx(sfxSmash);

    // ==== Death → Fade(Volume) → Interstitial → Load ====
    public void StartLoadNextSceneAfterDeath(string dieStateName = "Die", int layer = 0)
    {
        StartCoroutine(Co_LoadNextScene(dieStateName, layer));
    }

    IEnumerator Co_LoadNextScene(string dieStateName, int layer)
    {
        var anim = animator;
        float t = 0f, timeout = 4f;
        yield return null; // 트리거 적용 프레임 넘기기

        // 1) 죽음 상태 진입/완료 대기
        while (!anim.GetCurrentAnimatorStateInfo(layer).IsName(dieStateName) && t < timeout)
        { t += Time.unscaledDeltaTime; yield return null; }
        while (anim.GetCurrentAnimatorStateInfo(layer).normalizedTime < 0.98f && t < timeout)
        { t += Time.unscaledDeltaTime; yield return null; }

        // 2) 애니 완료 이후 경과시간을 누적하며 타이밍 분리
        float elapsed = 0f;
        bool uiShown  = false;

        // 안전: 페이드 시작이 UI 시작보다 빠르면 맞춰줌
        float uiAt   = Mathf.Max(0f, endingUiStartAfterDeath);
        float fadeAt = Mathf.Max(uiAt, fadeStartAfterDeath);

        while (elapsed < fadeAt)
        {
            elapsed += Time.unscaledDeltaTime;

            if (!uiShown && elapsed >= uiAt)
            {
                if (endingUI) endingUI.SetActive(true);
                uiShown = true;
                // 필요하면 여기서 SetNativeSize나 Sprite 교체 등 추가
            }

            yield return null;
        }

        // 3) 페이드아웃 시작
        var fader = FindObjectOfType<VolumeFader>();
        if (fader)      yield return fader.FadeOutRoutine(fadeDuration);
        else if (fadeCanvasFallback)
            yield return FadeCanvasGroup(fadeCanvasFallback, 1f, fadeDuration);

        // 4) 다음 씬 로드
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadSceneAsync(nextSceneName);
        else
            Debug.LogWarning("[Boss] nextSceneName이 비어있습니다.");
    }


    IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration)
    {
        if (!cg) yield break;
        float start = cg.alpha;
        if (duration <= 0f) { cg.alpha = target; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        cg.alpha = target;
    }

    // 공격 판정 토글
    public void EnableLeftAttack() => leftHand.enabled = true;
    public void EnableRightAttack() => rightHand.enabled = true;
    public void DisableLeftAttack() => leftHand.enabled = false;
    public void DisableRightAttack() => rightHand.enabled = false;
    public void EnableWeaponAttack() => bossWeapon.enabled = true;
    public void DisableWeaponAttack() => bossWeapon.enabled = false;
    public void EnableSmashAttack() => smashCollider.enabled = true;
    public void DisableSmashAttack() => smashCollider.enabled = false;
}
