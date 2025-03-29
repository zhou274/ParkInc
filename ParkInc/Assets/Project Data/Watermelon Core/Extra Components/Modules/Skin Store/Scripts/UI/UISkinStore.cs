using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;

namespace Watermelon.SkinStore
{
    public class UISkinStore : UIPage
    {
        private readonly float PANEL_BOTTOM_OFFSET_Y = -2000f;
        public readonly string STORE_ITEM_POOL_NAME = "StoreItem";

        [SerializeField] List<UISkinStoreTab> tabs;
        private Dictionary<TabData, UISkinStoreTab> tabsDictionary;

        [Space]
        [SerializeField] RectTransform storeAnimatedPanelRect;
        [SerializeField] Image storePanelBackground;
        [SerializeField] ScrollRect productsScroll;
        [SerializeField] GameObject scrollbarVertical;
        [SerializeField] Image scrollFadeImage;

        [Header("Prefabs")]
        [SerializeField] GameObject storeItemPrefab;

        [Header("Preview")]
        [SerializeField] CanvasGroup previewCanvasGroup;
        [SerializeField] Image backgroundImage;
        [SerializeField] Image previewImage;
        [SerializeField] RawImage previewRawImage;

        [Space]
        [SerializeField] Button closeButton;
        [SerializeField] CurrencyUIPanelSimple currencyPanel;
        [SerializeField] Button coinsForAdsButton;
        [SerializeField] TextMeshProUGUI coinsForAdsText;
        [SerializeField] Image coinsForAdsCurrencyImage;

        [Space]
        [SerializeField] UISkinItemsGrid storeGrid;
        [SerializeField] ScrollRect scrollView;

        private static PoolGeneric<UISkinItem> storeItemPool;
        private static float startedStorePanelRectPositionY;
        public string clickid;
        private StarkAdManager starkAdManager;
        private SkinData SelectedProductData { get; set; }

        private List<SkinData> currentPageProducts;
        private List<SkinData> lockedPageProducts = new List<SkinData>();

        private Currency rewardForAdsCurrency;

        public override void Initialise()
        {
            startedStorePanelRectPositionY = storeAnimatedPanelRect.anchoredPosition.y;

            storeItemPool = PoolManager.AddPool<UISkinItem>(new PoolSettings(STORE_ITEM_POOL_NAME, storeItemPrefab, 10, true));

            coinsForAdsText.text = "获得\n" + SkinStoreController.CoinsForAdsAmount;

            rewardForAdsCurrency = CurrenciesController.GetCurrency(SkinStoreController.CoinsForAdsCurrency);
            currencyPanel.Initialise();
        }

        public override void PlayShowAnimation()
        {
            previewCanvasGroup.alpha = 0;
            previewCanvasGroup.DOFade(1, 0.3f);

            InitStoreUI(true);

            storeAnimatedPanelRect.anchoredPosition = storeAnimatedPanelRect.anchoredPosition.SetY(PANEL_BOTTOM_OFFSET_Y);

            storeAnimatedPanelRect.DOAnchoredPosition(new Vector3(storeAnimatedPanelRect.anchoredPosition.x, startedStorePanelRectPositionY + 100f, 0f), 0.4f).SetEasing(Ease.Type.SineInOut).OnComplete(delegate
            {
                storeAnimatedPanelRect.DOAnchoredPosition(new Vector3(storeAnimatedPanelRect.anchoredPosition.x, startedStorePanelRectPositionY, 0f), 0.2f).SetEasing(Ease.Type.SineInOut).OnComplete(() =>
                {
                    UIController.OnPageOpened(this);
                });
            });

            closeButton.transform.localScale = Vector3.zero;
            closeButton.DOScale(1, 0.3f).SetEasing(Ease.Type.SineOut);

            currencyPanel.transform.localScale = Vector3.zero;
            currencyPanel.DOScale(1, 0.3f).SetEasing(Ease.Type.SineOut);

            coinsForAdsButton.interactable = true;
            coinsForAdsCurrencyImage.sprite = rewardForAdsCurrency.Icon;
        }

        private SkinPreview3D storePreview3D;

        public void InitStoreUI(bool resetScroll = false)
        {
            // Clear pools
            storeItemPool?.ReturnToPoolEverything(true);

            SelectedProductData = SkinStoreController.GetSelectedProductData(SkinStoreController.SelectedTabData);

            var tab = SkinStoreController.SelectedTabData;
            if (tab.PreviewType == PreviewType.Preview_2D)
            {
                previewRawImage.enabled = false;
                backgroundImage.enabled = true;
                previewImage.enabled = true;
            }
            else
            {
                previewRawImage.enabled = true;
                backgroundImage.enabled = false;
                previewImage.enabled = false;

                if (storePreview3D != null)
                    Destroy(storePreview3D.gameObject);

                storePreview3D = Instantiate(tab.PreviewPrefab).GetComponent<SkinPreview3D>();
                storePreview3D.Init();
                storePreview3D.SpawnProduct(SelectedProductData);

                previewRawImage.texture = storePreview3D.Texture;
            }

            previewImage.sprite = SelectedProductData.Preview2DSprite;
            backgroundImage.color = SkinStoreController.SelectedTabData.BackgroundColor;
            backgroundImage.sprite = SkinStoreController.SelectedTabData.BackgroundImage;

            InitPage(resetScroll);

            UpdateCurrentPage(true);

            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].SetSelectedStatus(tabs[i].Data == SkinStoreController.SelectedTabData);
            }
        }

        private void InitPage(bool resetScroll)
        {
            storeGrid.Init(SkinStoreController.GetProducts(SkinStoreController.SelectedTabData), SelectedProductData.UniqueId);

            if (resetScroll)
                scrollView.normalizedPosition = Vector2.up;
        }

        private void UpdateCurrentPage(bool redrawStorePage)
        {
            currentPageProducts = SkinStoreController.GetProducts(SkinStoreController.SelectedTabData);

            lockedPageProducts.Clear();

            for (int i = 0; i < currentPageProducts.Count; i++)
            {
                var product = currentPageProducts[i];

                if (!product.IsUnlocked && !product.IsDummy)
                {
                    lockedPageProducts.Add(product);
                }
            }

            if (redrawStorePage)
            {
                storeGrid.UpdateItems(SelectedProductData.UniqueId);
            }

            storePanelBackground.color = SkinStoreController.SelectedTabData.PanelColor;
            scrollFadeImage.color = SkinStoreController.SelectedTabData.PanelColor;

            productsScroll.enabled = currentPageProducts.Count > 6;
            scrollbarVertical.SetActive(currentPageProducts.Count > 6);
            scrollFadeImage.gameObject.SetActive(currentPageProducts.Count > 6);
        }

        public override void PlayHideAnimation()
        {
            closeButton.DOScale(0, 0.3f).SetEasing(Ease.Type.SineIn);
            currencyPanel.DOScale(0, 0.3f).SetEasing(Ease.Type.SineIn);

            if (storePreview3D != null)
            {
                Destroy(storePreview3D.gameObject);
            }

            previewCanvasGroup.DOFade(0, 0.3f);

            storeAnimatedPanelRect.DOAnchoredPosition(new Vector3(storeAnimatedPanelRect.anchoredPosition.x, startedStorePanelRectPositionY + 100f, 0f), 0.2f).SetEasing(Ease.Type.SineInOut).OnComplete(delegate
            {
                storeAnimatedPanelRect.DOAnchoredPosition(new Vector3(storeAnimatedPanelRect.anchoredPosition.x, PANEL_BOTTOM_OFFSET_Y, 0f), 0.4f).SetEasing(Ease.Type.SineInOut).OnComplete(delegate
                {
                    UIController.OnPageClosed(this);
                });
            });
        }

        public void InitTabs(TabData.SimpleTabDelegate OnTabClicked)
        {
            tabsDictionary = new Dictionary<TabData, UISkinStoreTab>();

            if (SkinStoreController.TabsCount > tabs.Count)
                Debug.LogError("[Store Module] Not enough tabs in store page ui");

            var tabsCount = Mathf.Min(SkinStoreController.TabsCount, tabs.Count);

            for (int i = 0; i < tabsCount; i++)
            {
                var tab = tabs[i];
                var tabData = SkinStoreController.GetTab(i);

                tab.gameObject.SetActive(true);
                tab.Init(tabData, OnTabClicked);

                tabsDictionary.Add(tabData, tab);
            }

            for (int i = tabsCount; i < tabs.Count; i++)
            {
                tabs[i].gameObject.SetActive(false);
            }
        }

        public void SetSelectedTab(TabData tabData)
        {
            foreach (var tab in tabs)
            {
                tab.SetSelectedStatus(tab.Data == tabData);
            }

            InitStoreUI(true);
        }

        //观看广告领取金币
        public void GetCoinsForAdsButton()
        {
            ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {
                    coinsForAdsButton.interactable = false;
                    FloatingCloud.SpawnCurrency(rewardForAdsCurrency.CurrencyType.ToString(), coinsForAdsText.rectTransform, currencyPanel.RectTransform, 20, "", () =>
                    {
                        CurrenciesController.Add(rewardForAdsCurrency.CurrencyType, SkinStoreController.CoinsForAdsAmount);

                        UpdateCurrentPage(true);

                        coinsForAdsButton.interactable = true;
                    });



                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
            
            
        }

        public void CloseButton()
        {
            UIController.HidePage<UISkinStore>(() =>
            {
                UIController.ShowPage<UIGame>();
            });
        }

        public static void EnablePreviewRawImage()
        {
            UIController.GetPage<UISkinStore>().previewRawImage.enabled = true;
        }

        public static void DisablePreviewRawImage()
        {
            UIController.GetPage<UISkinStore>().previewRawImage.enabled = false;
        }
        public void getClickid()
        {
            var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
            if (launchOpt.Query != null)
            {
                foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                    if (kv.Value != null)
                    {
                        Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                        if (kv.Key.ToString() == "clickid")
                        {
                            clickid = kv.Value.ToString();
                        }
                    }
                    else
                    {
                        Debug.Log(kv.Key + "<-参数-> " + "null ");
                    }
            }
        }

        public void apiSend(string eventname, string clickid)
        {
            TTRequest.InnerOptions options = new TTRequest.InnerOptions();
            options.Header["content-type"] = "application/json";
            options.Method = "POST";

            JsonData data1 = new JsonData();

            data1["event_type"] = eventname;
            data1["context"] = new JsonData();
            data1["context"]["ad"] = new JsonData();
            data1["context"]["ad"]["callback"] = clickid;

            Debug.Log("<-data1-> " + data1.ToJson());

            options.Data = data1.ToJson();

            TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
               response => { Debug.Log(response); },
               response => { Debug.Log(response); });
        }


        /// <summary>
        /// </summary>
        /// <param name="adId"></param>
        /// <param name="closeCallBack"></param>
        /// <param name="errorCallBack"></param>
        public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
        {
            starkAdManager = StarkSDK.API.GetStarkAdManager();
            if (starkAdManager != null)
            {
                starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
            }
        }
    }
}