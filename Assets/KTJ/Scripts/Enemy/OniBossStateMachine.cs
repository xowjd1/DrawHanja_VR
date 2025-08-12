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
    private bool  phase2Triggered = false;

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
    [SerializeField] private float attackRange    = 2f;
    [SerializeField] private float moveSpeed      = 6f;
    [SerializeField] private float rotationSpeed  = 5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxIntro;        // 인트로
    public AudioClip sfxPhase2Start;  // 2페 시작
    public AudioClip sfxSmash;        // 스매시(애니 이벤트로 호출)

    [Header("Scene Transition on Death")]
    public string nextSceneName = "NextScene";
    public float dieExtraDelay  = 0.6f;   // 애니 끝난 직후 잠깐 대기
    public CanvasGroup fadeCanvas;        // 검은 화면 CanvasGroup (alpha=0 시작)
    public float fadeDuration   = 0.8f;

    [Header("Interstitial UI (optional)")]
    public CanvasGroup interstitialGroup; // 중간에 보여줄 패널 (alpha=0 / 비활성 시작)
    public Image interstitialImage;
    public Sprite interstitialSprite;
    public float interstitialDelayBefore = 0.1f;
    public float interstitialFadeIn      = 0.35f;
    public float interstitialHoldTime    = 1.2f;
    public bool  interstitialWaitForInput = false;

    // 1페이즈
    public OniBossState CreateIntroState()         => new BossIntroState(this);
    public OniBossState CreateMoveState()          => new MoveToPlayerState(this);
    public OniBossState CreateAttack1State()       => new Boss1NorAttackState(this);
    public OniBossState CreateAttack2State()       => new Boss1NorAttack2State(this);
    // 2페이즈
    public OniBossState CreatePhase2Start()        => new Boss2PhaseStartState(this);
    public OniBossState CreateMoveToPlayer2Phase() => new MoveToPlayer2PhaseState(this);
    public OniBossState CreateBoss2NorAttack()     => new Boss2NorAttackState(this);
    public OniBossState CreateBoss2ComboAttack()   => new Boss2ComboAttackState(this);
    public OniBossState CreateBoss2SmashAttack()   => new Boss2SmashAttackState(this);
    public OniBossState CreateDieState()           => new BossDieState(this);

    // Exposed properties
    public Transform PlayerTransform => player.transform;
    public float     MoveSpeed       => moveSpeed;
    public float     RotationSpeed   => rotationSpeed;
    public float     AttackRange     => attackRange;
    public float     DetectionRange  => detectionRange;

    private void Awake()
    {
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        ChangeState(CreateIntroState());
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(OniBossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void OnPhase2StartFinished()
    {
        ChangeState(CreateMoveToPlayer2Phase());
    }
    
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
        {
            ChangeState(CreateMoveToPlayer2Phase());
        }
    }
    
    public void ApplyDamage(float dmg)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        // 2페 돌입
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
        weapon.transform.localScale    = Vector3.one;
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

    private IEnumerator DisableAfter(GameObject vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (vfxInstance != null)
            Destroy(vfxInstance);

        DisableWeaponAttack();
        DisableSmashAttack();
    }
    
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!clip) return;
        var src = audioSource;
        if (!src)
        {
            src = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 1f;   // 3D
        }
        src.PlayOneShot(clip, volume);
    }

    // 애니메이션 이벤트(스매시 임팩트 프레임)에서 호출
    public void Anim_PlaySmashSfx()
    {
        PlaySfx(sfxSmash);
    }

    // 죽음 애니 끝 → 페이드아웃 → 인터스티셜(옵션) → 다음 씬
    public void StartLoadNextSceneAfterDeath(string dieStateName = "Die", int layer = 0)
    {
        StartCoroutine(Co_LoadNextScene(dieStateName, layer));
    }

    private IEnumerator Co_LoadNextScene(string dieStateName, int layer)
    {
        var anim = animator;
        float t = 0f, timeout = 10f;
        yield return null; // 트리거 적용 프레임 넘기기

        // 1) 죽음 상태 진입 대기
        while (!anim.GetCurrentAnimatorStateInfo(layer).IsName(dieStateName) && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // 2) 죽음 애니 끝날 때까지 대기
        while (anim.GetCurrentAnimatorStateInfo(layer).normalizedTime < 0.98f && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3) 추가 지연
        if (dieExtraDelay > 0f)
            yield return new WaitForSecondsRealtime(dieExtraDelay);

        // 4) 페이드아웃(검은 화면)
        if (fadeCanvas)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.blocksRaycasts = true;
            yield return FadeCanvasGroup(fadeCanvas, 1f, fadeDuration);
        }

        // 5) 인터스티셜(있으면)
        if (interstitialGroup && interstitialImage)
        {
            if (interstitialDelayBefore > 0f)
                yield return new WaitForSecondsRealtime(interstitialDelayBefore);

            if (interstitialSprite) interstitialImage.sprite = interstitialSprite;

            interstitialGroup.gameObject.SetActive(true);
            interstitialGroup.alpha = 0f;
            interstitialGroup.blocksRaycasts = true;

            // 페이드 인
            yield return FadeCanvasGroup(interstitialGroup, 1f, interstitialFadeIn);

            // 입력 대기 or 고정 시간 유지
            if (interstitialWaitForInput)
            {
                while (!Input.anyKeyDown) yield return null;
            }
            else
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, interstitialHoldTime));
            }

            // (선택) 인터스티셜 페이드아웃 하고 싶다면 주석 해제
            // yield return FadeCanvasGroup(interstitialGroup, 0f, 0.25f);
        }

        // 6) 다음 씬 로드
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadSceneAsync(nextSceneName);
        else
            Debug.LogWarning("[Boss] nextSceneName이 비어있습니다.");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration)
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
    public void EnableLeftAttack()   => leftHand.enabled = true;
    public void EnableRightAttack()  => rightHand.enabled = true;
    public void DisableLeftAttack()  => leftHand.enabled = false;
    public void DisableRightAttack() => rightHand.enabled = false;
    public void EnableWeaponAttack() => bossWeapon.enabled = true;
    public void DisableWeaponAttack()=> bossWeapon.enabled = false;
    public void EnableSmashAttack()  => smashCollider.enabled = true;
    public void DisableSmashAttack() => smashCollider.enabled = false;
}
