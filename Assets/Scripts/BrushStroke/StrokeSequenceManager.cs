using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
public class StrokeSequenceManager : MonoBehaviour
{
    [Header("순서대로 등록 (1 -> 4)")]
    public List<StrokeSequence> sequences;

    private int currentIndex = 0;

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
        // ActivateChildren(sequence.transform); // 호출하지 않음, ResetSequence가 활성 상태 조절
    }

    public async UniTaskVoid OnAllSequencesComplete()
    {
        Debug.Log("모든 StrokeSequence 완료!");
        // 완료 후 추가 처리 여기에 작성
        await UniTask.Delay(3000);
        Destroy(gameObject);
    }

    private void DeactivateChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
        }
    }
}
