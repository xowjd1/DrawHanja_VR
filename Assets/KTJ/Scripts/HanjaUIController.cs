using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HanjaUIController : MonoBehaviour
{
   public HanjaDataBase hanjaDataBase;
   public Image hanjaImageUI;
   public GameObject ScrollUI;

   [Header("한자 인덱스")]
   public int currentHanjaIndex = 0;


   public GameObject katana;
   public GameObject dango;

   [SerializeField] private TreeFalling treeFalling;
   [SerializeField] private QuestManager questManager;

   public GameObject fireVFX;
   public GameObject crossTheRiver;
   public GameObject tree;

   void Start()
   {
      ShowPracticeImage(currentHanjaIndex);
      ScrollUI.SetActive(false);
   }

   public void ShowPracticeImage(int index)
   {
      if (hanjaDataBase == null || hanjaImageUI == null) return;

      if (index >= 0 && index < hanjaDataBase.allowedHanja.Length)
      {
         var hanjaData = hanjaDataBase.allowedHanja[index];
         if (hanjaData != null && hanjaData.practiceImage != null)
         {
            hanjaImageUI.sprite = hanjaData.practiceImage;
         }
      }
   }

   private void OnTriggerEnter(Collider other)
   {
      // 刀 표시 0
      if (other.gameObject == katana)
      {
         ShowPracticeImage(0); 
         ScrollUI.SetActive(true);
      }
      

      // 力 표시 1
      if (other.gameObject == dango)
      {
         ShowPracticeImage(1);
         ScrollUI.SetActive(true);
      }

      // 불화 6
      if (other.gameObject == fireVFX)
      {
         ShowPracticeImage(6);
         ScrollUI.SetActive(true);
      }
      // 물수 5 > 강을 건너고 나서
      if (other.gameObject == crossTheRiver)
      {
         ShowPracticeImage(5);
         ScrollUI.SetActive(true);
      }
      // 나무목 4 > 벨 나무랑 상호작용
      if (other.gameObject == tree)
      {
         ShowPracticeImage(4);
         ScrollUI.SetActive(true);
      }
   }

   private void Update()
   {
      
      /*// 벨참 3 > 나무가 쓰러지면 띄우기
      if (treeFalling.isFalling)
      {
         ShowPracticeImage(3);
         ScrollUI.SetActive(true);
      }*/
      
      // 개견 7
      if (questManager.quests[0].isCompleted)
      {
         ShowPracticeImage(7);
         ScrollUI.SetActive(true);
      }
      
      // 원숭이원 8
      if (questManager.quests[1].isCompleted)
      {
         ShowPracticeImage(8);
         ScrollUI.SetActive(true);
      }
      
      // 새조 9
      if (questManager.quests[2].isCompleted)
      {
         ShowPracticeImage(9);
         ScrollUI.SetActive(true);
      }
      
      
      
   }
   
   public void OpenPanelWithIndex(int index)
   {
      ShowPracticeImage(index); // 스프라이트 교체
      var sc = ScrollUI ? ScrollUI.GetComponent<ScrollUI>() : null;

      if (sc != null) sc.Open(index);
      else {
         // 혹시 잘못 연결돼 있으면 에러 확인용
         Debug.LogError("[HanjaUI] ScrollUI 참조에 ScrollUI 컴포넌트가 없습니다.");
         if (ScrollUI && !ScrollUI.activeSelf) ScrollUI.SetActive(true);
      }
   }

}
