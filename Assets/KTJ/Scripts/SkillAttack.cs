using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkillAttack : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private GameObject slashPrefab;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float forwardOffset = 1.0f;

    [Header("Pause Target")]
    [Tooltip("Enemy 태그를 쓰지 못할 때 수동으로 지정할 루트들(Animator/NavMeshAgent 등을 하위에서 찾아서 정지).")]
    [SerializeField] private List<GameObject> extraEnemyRoots = new();

    void Update()
    {
        // Z = Fire
        if (Input.GetKeyDown(KeyCode.Z))
            SpawnSkill(firePrefab);

        // X = Slash
        if (Input.GetKeyDown(KeyCode.X))
            SpawnSkill(slashPrefab);
    }

    void SpawnSkill(GameObject prefab)
    {
        if (!prefab)
        {
            Debug.LogWarning("[SkillAttack] Prefab not assigned.");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position + transform.forward * forwardOffset;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        var go = Instantiate(prefab, pos, rot);
        go.transform.localScale *= 2f;
        // 스킬 활성 카운트 ↑ & 적들 일시정지
        PauseController.IncrementAndPause(extraEnemyRoots);

        // 프리팹이 파괴될 때 재개하도록 리스너 부착
        var listener = go.AddComponent<SkillLifetimeListener>();
        listener.onDestroyed = () => PauseController.DecrementAndResumeIfNone();
    }

    /// <summary>스폰된 프리팹에 붙여, 파괴될 때 Pause 해제 카운트다운.</summary>
    private class SkillLifetimeListener : MonoBehaviour
    {
        public Action onDestroyed;
        void OnDestroy() { onDestroyed?.Invoke(); }
    }

    /// <summary>적들(Animator/NavMeshAgent)을 정지/재개하는 임시 컨트롤러.</summary>
    private static class PauseController
    {
        static int _activeSkills = 0;

        // 캐시
        static readonly Dictionary<Animator, float> _animSpeeds = new();
        static readonly HashSet<NavMeshAgent> _stoppedAgents = new();

        public static void IncrementAndPause(List<GameObject> extraRoots)
        {
            _activeSkills++;
            if (_activeSkills == 1)
                ApplyPause(extraRoots);
        }

        public static void DecrementAndResumeIfNone()
        {
            if (_activeSkills > 0) _activeSkills--;
            if (_activeSkills == 0)
                RemovePause();
        }

        static void ApplyPause(List<GameObject> extraRoots)
        {
            // 1) Enemy 태그 우선 탐색
            var enemyRoots = FindGameObjectsWithTagSafe("Enemy");

            // 2) 태그가 없다면 이름에 "Oni" 포함한 객체(임시) + 수동 루트 포함
            if (enemyRoots.Count == 0)
            {
                foreach (var a in GameObject.FindObjectsOfType<Animator>(true))
                {
                    if (a && a.gameObject.name.IndexOf("Oni", StringComparison.OrdinalIgnoreCase) >= 0)
                        enemyRoots.Add(a.gameObject);
                }
            }

            if (extraRoots != null)
                enemyRoots.AddRange(extraRoots);

            // 3) 하위의 Animator/NavMeshAgent 멈춤
            foreach (var root in enemyRoots)
            {
                if (!root) continue;

                foreach (var anim in root.GetComponentsInChildren<Animator>(true))
                {
                    if (!_animSpeeds.ContainsKey(anim))
                        _animSpeeds[anim] = anim.speed;
                    anim.speed = 0f;
                }

                foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true))
                {
                    if (!_stoppedAgents.Contains(agent))
                    {
                        agent.isStopped = true;
                        _stoppedAgents.Add(agent);
                    }
                }
            }

            Debug.Log($"[SkillAttack] Enemy PAUSED (targets={enemyRoots.Count})");
        }

        static void RemovePause()
        {
            // Animator speed 복구
            foreach (var kv in _animSpeeds)
                if (kv.Key) kv.Key.speed = kv.Value;
            _animSpeeds.Clear();

            // NavMeshAgent 재개
            foreach (var agent in _stoppedAgents)
                if (agent) agent.isStopped = false;
            _stoppedAgents.Clear();

            Debug.Log("[SkillAttack] Enemy RESUMED");
        }

        static List<GameObject> FindGameObjectsWithTagSafe(string tag)
        {
            var list = new List<GameObject>();
            try
            {
                list.AddRange(GameObject.FindGameObjectsWithTag(tag));
            }
            catch (UnityException)
            {
                // 태그가 프로젝트에 없을 때 예외 → 무시하고 빈 목록 반환
            }
            return list;
        }
    }
}
