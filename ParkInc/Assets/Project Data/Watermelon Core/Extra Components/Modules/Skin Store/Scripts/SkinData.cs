using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.SkinStore
{
    [System.Serializable]
    public class SkinData
    {
        [SerializeField] string uniqueId;
        public string UniqueId => uniqueId;

        // Editor
        [SerializeField] bool isExpanded;

        [Space]
        [SerializeField] int id;
        [SerializeField] int tabId;

        public int Id => id;
        public int TabId => tabId;
        public SkinTab Tab => (SkinTab)tabId;

        [Space]
        [SerializeField] bool isDummy;
        public bool IsDummy => isDummy;

        [Space]
        [SerializeField] string name;
        public string Name => name;

        [Space]
        [SerializeField] Sprite openedSprite;
        [SerializeField] Sprite lockedSprite;
        [SerializeField] Sprite preview2DSprite;

        public Sprite OpenedSprite => openedSprite;
        public Sprite LockedSprite => lockedSprite;
        public Sprite Preview2DSprite => preview2DSprite == null ? openedSprite : preview2DSprite;

        [Space]
        [SerializeField] GameObject prefab;
        [SerializeField] GameObject previewPrefab;

        public GameObject Prefab => prefab;
        public GameObject PreviewPrefab => previewPrefab == null ? prefab : previewPrefab;

        [Space]
        [SerializeField] PurchaseType purchaseType;
        [SerializeField] CurrencyType currency;
        [SerializeField] int cost = 1;

        public PurchaseType PurchType => purchaseType;
        public CurrencyType Currency => currency;
        public int Cost => cost;

        private SkinSave save;

        public bool IsUnlocked
        {
            get => save.isUnlocked;
            set => save.isUnlocked = value;
        }

        public int RewardedVideoWatchedAmount
        {
            get => save.rewardedVideoWatchedAmount;
            set => save.rewardedVideoWatchedAmount = value;
        }

        public class SkinSave : ISaveObject
        {
            public bool isUnlocked;
            public int rewardedVideoWatchedAmount;

            public SkinSave()
            {
                isUnlocked = false;
                rewardedVideoWatchedAmount = 0;
            }

            public void Flush()
            {
            }
        }

        public void Init()
        {
            save = SaveController.GetSaveObject<SkinSave>("Skin :" + uniqueId);

            if(RewardedVideoWatchedAmount >= cost)
                save.isUnlocked = true;
        }

        public enum PurchaseType
        {
            InGameCurrency = 0,
            RewardedVideo = 1,
        }
    }
}