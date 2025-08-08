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

   private TreeFalling treeFalling;
   private QuestManager questManager;

   public GameObject fireVFX;
   private bool _prevFireActive;

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
   }

   private void Update()
   {
      
      // 斬 표시 
      if (treeFalling.isFalling)
      {
         ShowPracticeImage(2); 
      }
      // 火 표시    
      if (fireVFX != null)
      {
         bool isFireActive = fireVFX.activeSelf;
         if (isFireActive && !_prevFireActive)
         {
            ShowPracticeImage(5);
            ScrollUI.SetActive(true);
         }
         _prevFireActive = isFireActive;
      }
      // 猿 표시 8
      if (questManager.quests[1].isCompleted)
      {
         ShowPracticeImage(4);
         
      }
      // 鳥 표시 
      if (questManager.quests[2].isCompleted)
      {
         ShowPracticeImage(9); 
      }
   }



   // 木 표시 3
   // 炎 표시 6
   // 犬 표시 7
   
   // 鳥 표시 10


}
