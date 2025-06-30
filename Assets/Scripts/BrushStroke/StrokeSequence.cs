using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class StrokeSequence : MonoBehaviour
{
    public List<StrokePoint> points;
    public List<StrokePath> paths;
    // [SerializeField] private AudioParticle audioParticle;

    void Awake()
    {
        points = GetComponentsInChildren<StrokePoint>(true).OrderBy(p => p.index).ToList();
        paths = GetComponentsInChildren<StrokePath>(true).ToList();

        foreach (var p in points)
        {
            p.sequence = this;
        }

        ResetSequence();

        // audioParticle.Visual += audioParticle.PlayLeftVibration;
        // audioParticle.Visual += audioParticle.PlayRightVibration;
        // audioParticle.Visual += audioParticle.PlayAudio;
        // audioParticle.Visual += audioParticle.PlayWaveParticle;
    }

    public void ResetSequence()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i].isHit = false;
            points[i].isActive = (i == 0);
        }

        foreach (var path in paths)
        {
            path.isHit = false;
        }

        UpdateActiveStates(); // 이 줄 추가
    }

    public void OnPointHit(StrokePoint point)
    {
        point.isHit = true;
        point.isActive = false;  // 마지막 포인트도 꺼짐 처리

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
        print("StrokeSequence 완료");
        // audioParticle.Visual();
        gameObject.SetActive(false);
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
            bool isActive = points[i].isActive;
            points[i].gameObject.SetActive(isActive);

            // 패스 활성화: i번째 패스는 (i+1)번째 포인트의 isActive 상태에 따라 결정
            if (i < paths.Count)
            {
                bool nextPointActive = (i + 1 < points.Count) ? points[i + 1].isActive : false;
                paths[i].gameObject.SetActive(nextPointActive);
            }
        }

        // 남은 패스는 강제로 꺼줌
        for (int i = points.Count; i < paths.Count; i++)
        {
            paths[i].gameObject.SetActive(false);
        }
    }

}
