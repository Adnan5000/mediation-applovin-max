using com.adjust.sdk;
using MindfreakStudios.Scripts.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MindfreakStudios.Scripts.Ads
{
    public class AdManager : MonoSingleton<AdManager>
    {

        [Header("Banner Position")]
        [SerializeField] private MaxSdk.BannerPosition bannerPosition;
        [Header("MREC Position")]
        [SerializeField] MaxSdkBase.AdViewPosition mRECPosition;

        [Header("Max Key")]
        [SerializeField] private string MaxSdkKey = "KPlIpg_x0W5xfPX0p58p3FLLTfQcvyCELyX9dW4d1AZhZ5hNBrBE5XaeC_F5xSKHOsQ1MUfNfD3EImbUbokgJI";

        [Header("Ad IDs")]
#if UNITY_IOS
    [SerializeField]private string InterstitialAdUnitId = "ENTER_IOS_INTERSTITIAL_AD_UNIT_ID_HERE";
    [SerializeField]private string RewardedAdUnitId = "ENTER_IOS_REWARD_AD_UNIT_ID_HERE";
    [SerializeField]private string RewardedInterstitialAdUnitId = "ENTER_IOS_REWARD_INTER_AD_UNIT_ID_HERE";
    [SerializeField]private string BannerAdUnitId = "ENTER_IOS_BANNER_AD_UNIT_ID_HERE";
    [SerializeField]private string MRecAdUnitId = "ENTER_IOS_MREC_AD_UNIT_ID_HERE";
#else // UNITY_ANDROID
        [SerializeField] private string InterstitialAdUnitId = "52c5672c0d85ac84";
        [SerializeField] private string RewardedAdUnitId = "5740f980d68a926b";
        [SerializeField] private string RewardedInterstitialAdUnitId = "ENTER_ANDROID_REWARD_INTER_AD_UNIT_ID_HERE";
        [SerializeField] private string BannerAdUnitId = "5b0968aff3d464aa";
        [SerializeField] private string MRecAdUnitId = "2812f4253555b165";
#endif
        public Button mediationDebuggerButton;
        public Text interstitialStatusText;
        public Text rewardedStatusText;
        public Text rewardedInterstitialStatusText;

        private int interstitialRetryAttempt;
        private int rewardedRetryAttempt;
        private int rewardedInterstitialRetryAttempt;

        void Start()
        {

            mediationDebuggerButton.onClick.AddListener(MaxSdk.ShowMediationDebugger);
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");

                InitializeInterstitialAds();
                InitializeRewardedAds();
                InitializeRewardedInterstitialAds();
                InitializeBannerAds();
                InitializeMRecAds();

                // Initialize Adjust SDK
                AdjustConfig adjustConfig = new AdjustConfig("YourAppToken", AdjustEnvironment.Sandbox);
                Adjust.start(adjustConfig);
            };

            MaxSdk.SetSdkKey(MaxSdkKey);
            MaxSdk.InitializeSdk();
        }

        #region Interstitial Ad Methods

        private void InitializeInterstitialAds()
        {
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;

            // Load the first interstitial
            LoadInterstitial();
        }

        void LoadInterstitial()
        {
            if(interstitialStatusText)
            interstitialStatusText.text = "Loading...";
            Debug.Log("AdManager: Interstitial Loading...");
            MaxSdk.LoadInterstitial(InterstitialAdUnitId);
        }

        public void ShowInterstitial()
        {
            if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
            {
                if (interstitialStatusText)
                    interstitialStatusText.text = "Showing";
                Debug.Log("AdManager: Interstitial Showing...");
                MaxSdk.ShowInterstitial(InterstitialAdUnitId);
            }
            else
            {
                if (interstitialStatusText)
                    interstitialStatusText.text = "Ad not ready";
                Debug.LogError("AdManager: Ad Not Ready...");
            }
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
            if (interstitialStatusText)
                interstitialStatusText.text = "Loaded";
            Debug.Log("AdManager: Interstitial Loaded");

            // Reset retry attempt
            interstitialRetryAttempt = 0;
        }

        private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            interstitialRetryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));
            if (interstitialStatusText)
                interstitialStatusText.text = "Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...";

            Debug.Log("AdManager: Interstitial Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");

            Invoke("LoadInterstitial", (float)retryDelay);
        }

        private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad failed to display. We recommend loading the next ad
            Debug.Log("AdManager: Interstitial failed to display with error code: " + errorInfo.Code);
            LoadInterstitial();
        }

        private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is hidden. Pre-load the next ad
            Debug.Log("AdManager: Interstitial dismissed");
            LoadInterstitial();
        }

        private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad revenue paid. Use this callback to track user revenue.
            Debug.Log("AdManager: Interstitial revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            TrackAdRevenue(adInfo);
        }

        #endregion

        #region Rewarded Ad Methods

        private void InitializeRewardedAds()
        {
            // Attach callbacks
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

            // Load the first RewardedAd
            LoadRewardedAd();
        }

        private void LoadRewardedAd()
        {
            if(rewardedStatusText)
            rewardedStatusText.text = "Loading...";

            Debug.Log("AdManager: Rewarded Loading...");
            MaxSdk.LoadRewardedAd(RewardedAdUnitId);
        }

        public void ShowRewardedAd()
        {
            if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
            {
                if (rewardedStatusText)
                    rewardedStatusText.text = "Showing";

                Debug.Log("AdManager: Rewarded Showing...");
                MaxSdk.ShowRewardedAd(RewardedAdUnitId);
            }
            else
            {
                if (rewardedStatusText)
                    rewardedStatusText.text = "Ad not ready";

                Debug.LogError("AdManager: Rewarded Ad not ready...");

            }
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
            if (rewardedStatusText)
                rewardedStatusText.text = "Loaded";

            Debug.Log("AdManager: Rewarded ad loaded");

            // Reset retry attempt
            rewardedRetryAttempt = 0;
        }

        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            rewardedRetryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));
            if (rewardedStatusText)
                rewardedStatusText.text = "Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...";

            Debug.Log("AdManager: Rewarded ad Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");

            Invoke("LoadRewardedAd", (float)retryDelay);
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad failed to display. We recommend loading the next ad
            Debug.Log("AdManager: Rewarded ad failed to display with error code: " + errorInfo.Code);
            LoadRewardedAd();
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("AdManager: Rewarded ad displayed");
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("AdManager: Rewarded ad clicked");
        }

        private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is hidden. Pre-load the next ad
            Debug.Log("AdManager: Rewarded ad dismissed");
            LoadRewardedAd();
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad was displayed and user should receive the reward
            Debug.Log("AdManager: Rewarded ad received reward");
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad revenue paid. Use this callback to track user revenue.
            Debug.Log("AdManager: Rewarded ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            TrackAdRevenue(adInfo);
        }

        #endregion

        #region Rewarded Interstitial Ad Methods

        private void InitializeRewardedInterstitialAds()
        {
            // Attach callbacks
            MaxSdkCallbacks.RewardedInterstitial.OnAdLoadedEvent += OnRewardedInterstitialAdLoadedEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdLoadFailedEvent += OnRewardedInterstitialAdFailedEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayFailedEvent += OnRewardedInterstitialAdFailedToDisplayEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayedEvent += OnRewardedInterstitialAdDisplayedEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdClickedEvent += OnRewardedInterstitialAdClickedEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdHiddenEvent += OnRewardedInterstitialAdDismissedEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdReceivedRewardEvent += OnRewardedInterstitialAdReceivedRewardEvent;
            MaxSdkCallbacks.RewardedInterstitial.OnAdRevenuePaidEvent += OnRewardedInterstitialAdRevenuePaidEvent;

            // Load the first RewardedInterstitialAd
            LoadRewardedInterstitialAd();
        }

        private void LoadRewardedInterstitialAd()
        {
            if(rewardedInterstitialStatusText)
            rewardedInterstitialStatusText.text = "Loading...";
            Debug.Log("AdManager: Rewarded Interstitial Loading...");
            MaxSdk.LoadRewardedInterstitialAd(RewardedInterstitialAdUnitId);
        }

        public void ShowRewardedInterstitialAd()
        {
            if (MaxSdk.IsRewardedInterstitialAdReady(RewardedInterstitialAdUnitId))
            {
                if (rewardedInterstitialStatusText)
                    rewardedInterstitialStatusText.text = "Showing";

                Debug.Log("AdManager: Rewarded Interstitial Showing...");
                MaxSdk.ShowRewardedInterstitialAd(RewardedInterstitialAdUnitId);
            }
            else
            {
                if (rewardedInterstitialStatusText)
                    rewardedInterstitialStatusText.text = "Ad not ready";
                Debug.LogError("Rewarded Interstitial Ad not ready...");
            }
        }

        private void OnRewardedInterstitialAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded interstitial ad is ready to be shown. MaxSdk.IsRewardedInterstitialAdReady(rewardedInterstitialAdUnitId) will now return 'true'
            if (rewardedInterstitialStatusText)
                rewardedInterstitialStatusText.text = "Loaded";
            Debug.Log("AdManager: Rewarded interstitial ad loaded");

            // Reset retry attempt
            rewardedInterstitialRetryAttempt = 0;
        }

        private void OnRewardedInterstitialAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Rewarded interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            rewardedInterstitialRetryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, rewardedInterstitialRetryAttempt));
            if (rewardedInterstitialStatusText)
                rewardedInterstitialStatusText.text = "Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...";

            Debug.Log("AdManager: Rewarded interstitial ad Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");

            Invoke("LoadRewardedInterstitialAd", (float)retryDelay);
        }

        private void OnRewardedInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded interstitial ad failed to display. We recommend loading the next ad
            Debug.Log("AdManager: Rewarded interstitial ad failed to display with error code: " + errorInfo.Code);
            LoadRewardedInterstitialAd();
        }

        private void OnRewardedInterstitialAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("AdManager: Rewarded interstitial ad displayed");
        }

        private void OnRewardedInterstitialAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("AdManager: Rewarded interstitial ad clicked");
        }

        private void OnRewardedInterstitialAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded interstitial ad is hidden. Pre-load the next ad
            Debug.Log("AdManager: Rewarded interstitial ad dismissed");
            LoadRewardedInterstitialAd();
        }

        private void OnRewardedInterstitialAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded interstitial ad was displayed and user should receive the reward
            Debug.Log("AdManager: Rewarded interstitial ad received reward");
        }

        private void OnRewardedInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded interstitial ad revenue paid. Use this callback to track user revenue.
            Debug.Log("AdManager: Rewarded interstitial ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            TrackAdRevenue(adInfo);
        }

        #endregion

        #region Banner Ad Methods

        private void InitializeBannerAds()
        {
            // Attach Callbacks
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

            // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
            // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
            MaxSdk.CreateBanner(BannerAdUnitId, bannerPosition);

            // Set background or background color for banners to be fully functional.
            MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
        }

        public void ShowBanner(bool show)
        {
            if (show)
            {
                MaxSdk.ShowBanner(BannerAdUnitId);
                //showBannerButton.GetComponentInChildren<Text>().text = "Hide Banner";
            }
            else
            {
                MaxSdk.HideBanner(BannerAdUnitId);
                //showBannerButton.GetComponentInChildren<Text>().text = "Show Banner";
            }

        }

        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad is ready to be shown.
            // If you have already called MaxSdk.ShowBanner(BannerAdUnitId) it will automatically be shown on the next ad refresh.
            Debug.Log("AdManager: Banner ad loaded");
        }

        private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Banner ad failed to load. MAX will automatically try loading a new ad internally.
            Debug.Log("AdManager: Banner ad failed to load with error code: " + errorInfo.Code);
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("AdManager: Banner ad clicked");
        }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad revenue paid. Use this callback to track user revenue.
            Debug.Log("AdManager: Banner ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            TrackAdRevenue(adInfo);
        }

        #endregion

        #region MREC Ad Methods

        private void InitializeMRecAds()
        {
            // Attach Callbacks
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;

            // MRECs are automatically sized to 300x250.
            MaxSdk.CreateMRec(MRecAdUnitId, mRECPosition);
        }

        public void ShowMRec(bool show)
        {
            if (show)
            {
                MaxSdk.ShowMRec(MRecAdUnitId);
                //showMRecButton.GetComponentInChildren<Text>().text = "Hide MREC";
            }
            else
            {
                MaxSdk.HideMRec(MRecAdUnitId);
                //showMRecButton.GetComponentInChildren<Text>().text = "Show MREC";
            }

        }

        private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MRec ad is ready to be shown.
            // If you have already called MaxSdk.ShowMRec(MRecAdUnitId) it will automatically be shown on the next MRec refresh.
            Debug.Log("AdManager: MRec ad loaded");
        }

        private void OnMRecAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // MRec ad failed to load. MAX will automatically try loading a new ad internally.
            Debug.Log("AdManager: MRec ad failed to load with error code: " + errorInfo.Code);
        }

        private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("AdManager: MRec ad clicked");
        }

        private void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MRec ad revenue paid. Use this callback to track user revenue.
            Debug.Log("AdManager: MRec ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            TrackAdRevenue(adInfo);
        }

        #endregion


        private void TrackAdRevenue(MaxSdkBase.AdInfo adInfo)
        {
            AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAppLovinMAX);

            adjustAdRevenue.setRevenue(adInfo.Revenue, "USD");
            adjustAdRevenue.setAdRevenueNetwork(adInfo.NetworkName);
            adjustAdRevenue.setAdRevenueUnit(adInfo.AdUnitIdentifier);
            adjustAdRevenue.setAdRevenuePlacement(adInfo.Placement);

            Adjust.trackAdRevenue(adjustAdRevenue);
        }
    }
}
