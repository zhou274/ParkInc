using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Watermelon.SkinStore;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;
using System.Collections.Generic;

namespace Watermelon
{
    public class UIGame : UIPage
    {
        [SerializeField] Image blackFadePanel;
        [SerializeField] Canvas blackFadeCanvas;

        [Header("Buttons")]
        [SerializeField] Button replayButton;
        [SerializeField] Button skipButton;

        [Header("Coins")]
        [SerializeField] TMP_Text levelText;
        public string clickid;
        private StarkAdManager starkAdManager;
        public void ReplayButton()
        {
            if (!UITouchHandler.CanReplay) return;
            if (!LevelController.IsReplayAvailable()) return;

            GameController.ReplayLevel();

            SetReplayButtonVisibility(false);

            GameAudioController.PlayButtonAudio();
        }

        public void SetLevelText(int level)
        {
            levelText.text = "关卡 " + (level + 1);
        }
        //跳过按钮
        public void SkipButton()
        {
            ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {
                    GameAudioController.PlayButtonAudio();
                    GameController.TurnsAfterRewardVideo = 0;
                    GameController.SkipLevel();



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

        public void FirstLevelButton()
        {
            if (GameController.WinStage) return;
            GameController.FirstLevelDev();
        }

        public void NextLevelButton()
        {
            if (GameController.WinStage) return;
            //GameController.SkipLevel();
            LevelController.DestroyLevel();
            GameController.NextLevel(false);
        }

        public void PreviousLevel()
        {
            if (GameController.WinStage) return;
            GameController.PrevLevelDev();
        }

        public void SetReplayButtonVisibility(bool isShown)
        {
            if (isShown)
            {
                replayButton.image.rectTransform.DOAnchoredPosition(Vector3.up * 470f, 0.5f).SetEasing(Ease.Type.QuadOut);
            }
            else
            {
                replayButton.image.rectTransform.DOAnchoredPosition(new Vector2(200, 470), 0.5f).SetEasing(Ease.Type.QuadOut);
            }
        }

        public void SetSkipButtonVisibility(bool isShown)
        {
            if (isShown)
            {
                skipButton.image.rectTransform.DOAnchoredPosition(Vector3.up * 470f, 0.5f).SetEasing(Ease.Type.QuadOut);
            }
            else
            {
                skipButton.image.rectTransform.DOAnchoredPosition(new Vector2(-200, 470), 0.5f).SetEasing(Ease.Type.QuadOut);
            }
        }

        public void ShopButton()
        {
            GameAudioController.PlayButtonAudio();

            SkinStoreController.OpenStore();
        }

        public override void Initialise()
        {

        }

        #region Show/Hide

        public override void PlayHideAnimation()
        {
            UIController.OnPageClosed(this);
        }

        public override void PlayShowAnimation()
        {
            UIController.OnPageOpened(this);
        }

        #endregion

        private void BlackFadeOut()
        {
            blackFadeCanvas.enabled = true;
            blackFadePanel.color = Color.black;
            blackFadePanel.DOFade(0, 0.5f).OnComplete(() => {
                blackFadeCanvas.enabled = false;
            });
        }

        public void DoHidden(UnityAction action)
        {
            blackFadeCanvas.enabled = true;

            blackFadePanel.DOFade(1, 0.3f).OnComplete(() => {
                action();
                blackFadePanel.DOFade(0, 0.3f).OnComplete(() => {
                    blackFadeCanvas.enabled = false;
                });
            });
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
