using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Settings")]
    public float delay              = 3f;
    [SerializeField] private float detectionRange    = 10f;
    [SerializeField] private float attackRange       = 2f;
    [SerializeField] private float moveSpeed         = 6f;
    [SerializeField] private float rotationSpeed     = 5f;
    
    [SerializeField] private GameObject smashVFXPrefab;
    [SerializeField] private SphereCollider smashCollider;
    [SerializeField] private Transform smashVFXSpawnPoint;
    
    [Header("Audio")]
    public AudioSource audioSource;      // 보스 루트에 붙은 AudioSource 할당
    public AudioClip sfxIntro;           // 인트로 SFX
    public AudioClip sfxPhase2Start;     // 2페이즈 시작 SFX
    public AudioClip sfxSmash;           // 스매시 SFX (애니 이벤트로 호출)

    
    [Header("Scene Transition on Death")]
    public string nextSceneName = "NextScene"; // ← 인스펙터에서 지정
    public float dieExtraDelay = 0.6f;         // 애니 끝난 뒤 추가로 잠깐 멈춤
    public CanvasGroup fadeCanvas;             // 전체화면 검은 이미지(+CanvasGroup)
    public float fadeDuration = 0.8f;  
    
    
    // 1페이즈
    public OniBossState CreateIntroState()      => new BossIntroState(this);
    public OniBossState CreateMoveState()       => new MoveToPlayerState(this);
    public OniBossState CreateAttack1State()    => new Boss1NorAttackState(this);
    public OniBossState CreateAttack2State()    => new Boss1NorAttack2State(this);
    
    
    // 2페이즈
    public OniBossState CreatePhase2Start()     => new Boss2PhaseStartState(this);
    public OniBossState CreateMoveToPlayer2Phase()     => new MoveToPlayer2PhaseState(this);
    public OniBossState CreateBoss2NorAttack()     => new Boss2NorAttackState(this);
    public OniBossState CreateBoss2ComboAttack()     => new Boss2ComboAttackState(this);
    public OniBossState CreateBoss2SmashAttack()     => new Boss2SmashAttackState(this);
    public OniBossState CreateDieState()        => new BossDieState(this);

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
        // 2페 추적 스테이트로 넘어간다
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

        // 체력 50% 이하 & 아직 2페 진입 안 했으면
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
        // 무기를 등에서 손으로 옮기기
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
            // 안전장치: 없으면 자동 부착
            src = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;   // 3D 사운드
        }
        src.PlayOneShot(clip, volume);
    }

    // 애니메이션 이벤트에서 호출할 함수
    // (Smash 애니 타이밍 프레임에 이 함수명을 이벤트로 넣어주면 됨)
    public void Anim_PlaySmashSfx()
    {
        PlaySfx(sfxSmash);
    }


    public void StartLoadNextSceneAfterDeath(string dieStateName = "Die", int layer = 0)
    {
        StartCoroutine(Co_LoadNextScene(dieStateName, layer));
    }

    System.Collections.IEnumerator Co_LoadNextScene(string dieStateName, int layer)
    {
        // 1) 현재 레이어가 Die로 진입할 때까지 대기
        var anim = animator;
        float t = 0f, timeout = 10f;
        yield return null; // 트리거 적용 프레임 넘기기

        while (!anim.GetCurrentAnimatorStateInfo(layer).IsName(dieStateName) && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // 2) Die normalizedTime 거의 끝날 때까지 대기
        while (anim.GetCurrentAnimatorStateInfo(layer).normalizedTime < 0.98f && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3) 추가 지연
        if (dieExtraDelay > 0f)
            yield return new WaitForSecondsRealtime(dieExtraDelay);

        // 4) 페이드아웃
        if (fadeCanvas)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.blocksRaycasts = true;
            float a = 0f;
            while (a < 1f)
            {
                a += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration);
                fadeCanvas.alpha = Mathf.Clamp01(a);
                yield return null;
            }
        }

        // 5) 씬 로드
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadSceneAsync(nextSceneName);
        else
            Debug.LogWarning("[Boss] nextSceneName이 비어있습니다.");
    }

    // 애니메이션 이벤트로도 부를 수 있게(선택)
    public void Anim_OnBossDeathFinished()
    {
        StartLoadNextSceneAfterDeath();
    }
    
    
    
    public void EnableLeftAttack()  => leftHand.enabled = true;
    public void EnableRightAttack() => rightHand.enabled = true;
    public void DisableLeftAttack() => leftHand.enabled = false;
    public void DisableRightAttack() => rightHand.enabled = false;
    public void EnableWeaponAttack() => bossWeapon.enabled = true;
    public void DisableWeaponAttack() => bossWeapon.enabled = false;
    public void EnableSmashAttack() => smashCollider.enabled = true;
    public void DisableSmashAttack() => smashCollider.enabled = false;

}