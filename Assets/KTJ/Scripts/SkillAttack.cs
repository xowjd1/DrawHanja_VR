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
    [SerializeField] private List<GameObject> extraEnemyRoots = new();

    public BlackWhitePostProcess bwp;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) SpawnSkill(firePrefab);
        if (Input.GetKeyDown(KeyCode.X)) SpawnSkill(slashPrefab);
    }

    void SpawnSkill(GameObject prefab)
    {
        if (!prefab) { Debug.LogWarning("[SkillAttack] Prefab not assigned."); return; }

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position + transform.forward * forwardOffset;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        var go = Instantiate(prefab, pos, rot);

        // ⬇️ 정지/해제 전환 지점에서만 BW 토글되도록 콜백 전달
        PauseController.IncrementAndPause(
            extraEnemyRoots,
            onPaused:  () => { if (bwp) bwp.BlackWhite(); },
            onResumed: () => { if (bwp) bwp.BlackWhite(); }
        );

        var listener = go.AddComponent<SkillLifetimeListener>();
        listener.onDestroyed = () => PauseController.DecrementAndResumeIfNone();
    }

    private class SkillLifetimeListener : MonoBehaviour
    {
        public Action onDestroyed;
        void OnDestroy() { onDestroyed?.Invoke(); }
    }

    private static class PauseController
    {
        static int _activeSkills = 0;

        // Cache
        static readonly Dictionary<Animator, float> _animSpeeds = new();
        static readonly HashSet<NavMeshAgent> _stoppedAgents = new();

        // 이번 "정지 세션"에서만 사용할 콜백(중첩 스킬 시 1회만 호출)
        static Action _onPaused, _onResumed;

        public static void IncrementAndPause(
            List<GameObject> extraRoots,
            Action onPaused = null,
            Action onResumed = null)
        {
            _activeSkills++;
            if (_activeSkills == 1)
            {
                _onPaused  = onPaused;
                _onResumed = onResumed;
                ApplyPause(extraRoots);
                _onPaused?.Invoke(); // 🔊 BW 토글(정지 시작)
            }
        }

        public static void DecrementAndResumeIfNone()
        {
            if (_activeSkills > 0) _activeSkills--;
            if (_activeSkills == 0)
            {
                RemovePause();
                _onResumed?.Invoke(); // 🔊 BW 토글(정지 해제)
                _onPaused  = null;
                _onResumed = null;
            }
        }

        static void ApplyPause(List<GameObject> extraRoots)
        {
            var enemyRoots = FindGameObjectsWithTagSafe("Enemy");
            if (enemyRoots.Count == 0)
            {
                foreach (var a in GameObject.FindObjectsOfType<Animator>(true))
                    if (a && a.gameObject.name.IndexOf("Oni", StringComparison.OrdinalIgnoreCase) >= 0)
                        enemyRoots.Add(a.gameObject);
            }
            if (extraRoots != null) enemyRoots.AddRange(extraRoots);

            foreach (var root in enemyRoots)
            {
                if (!root) continue;

                foreach (var anim in root.GetComponentsInChildren<Animator>(true))
                {
                    if (!_animSpeeds.ContainsKey(anim)) _animSpeeds[anim] = anim.speed;
                    anim.speed = 0f;
                }
                foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true))
                {
                    if (_stoppedAgents.Add(agent))
                        agent.isStopped = true;
                }
            }
            Debug.Log($"[SkillAttack] Enemy PAUSED (targets={enemyRoots.Count})");
        }

        static void RemovePause()
        {
            foreach (var kv in _animSpeeds)
                if (kv.Key) kv.Key.speed = kv.Value;
            _animSpeeds.Clear();

            foreach (var agent in _stoppedAgents)
                if (agent) agent.isStopped = false;
            _stoppedAgents.Clear();

            Debug.Log("[SkillAttack] Enemy RESUMED");
        }

        static List<GameObject> FindGameObjectsWithTagSafe(string tag)
        {
            var list = new List<GameObject>();
            try { list.AddRange(GameObject.FindGameObjectsWithTag(tag)); }
            catch (UnityException) { }
            return list;
        }
    }
}
