using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

public class StrokeSequence : MonoBehaviour
{
    public List<StrokePoint> points;
    public List<StrokePath> paths;
    [HideInInspector] public StrokeSequenceManager manager;
    [SerializeField] private AudioParticle audioParticle;

    private Renderer rd;

    void Awake()
    {
        points = GetComponentsInChildren<StrokePoint>(true).OrderBy(p => p.index).ToList();
        paths = GetComponentsInChildren<StrokePath>(true).ToList();
        rd = GetComponent<Renderer>();

        foreach (var p in points)
        {
            p.sequence = this;
        }

        // audioParticle.Visual += audioParticle.PlayLeftVibration;
        // audioParticle.Visual += audioParticle.PlayRightVibration;
        audioParticle.Visual += audioParticle.PlayAudio;
        audioParticle.Visual += audioParticle.PlayParticle;
        audioParticle.Visual += audioParticle.DestroyParticle;
    }

    public void ResetSequence()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i].isHit = false;
            points[i].isActive = (i == 0); // 첫 번째 포인트만 활성화
        }

        foreach (var path in paths)
        {
            path.isHit = false;
        }

        UpdateActiveStates();
    }

    public void OnPointHit(StrokePoint point)
    {
        point.isHit = true;
        point.isActive = false;

        int nextIndex = point.index + 1;

        if (nextIndex < points.Count)
        {
            points[nextIndex].isActive = true;
        }
        else
        {
            OnComplete().Forget();
        }

        UpdateActiveStates();
    }

    public async UniTaskVoid OnComplete()
    {
        Debug.Log($"{gameObject.name} 완료");

        // gameObject.SetActive(false);
        if (rd != null)
        {
            rd.enabled = false;
        }
        else
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var r in childRenderers)
            {
                if (r != null)
                    r.enabled = false;
            }
        }
            audioParticle.Visual();
            DeactivateChildren();
            manager?.ActivateNextSequence();
            await UniTask.Delay(3000);
            Destroy(gameObject);
        
    }

    public void OnPathMissed()
    {
        ResetSequence();
    }

    private void UpdateActiveStates()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i].gameObject.SetActive(points[i].isActive);

            if (i < paths.Count)
            {
                bool nextPointActive = (i + 1 < points.Count) && points[i + 1].isActive;
                paths[i].gameObject.SetActive(nextPointActive);
            }
        }

        for (int i = points.Count; i < paths.Count; i++)
        {
            paths[i].gameObject.SetActive(false);
        }
    }

    public void DeactivateChildren()
    {
        foreach (var point in points)
        {
            if (point != null)
                point.gameObject.SetActive(false);
        }

        foreach (var path in paths)
        {
            if (path != null)
                path.gameObject.SetActive(false);
        }
    }
}
