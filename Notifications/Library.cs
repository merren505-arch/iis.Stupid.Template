using BepInEx;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static StupidTemplate.Settings;

namespace StupidTemplate.Notifications
{
    [BepInPlugin("org.gorillatag.lars.notifications2", "NotificationLibrary", "1.0.5")]
    public class NotifiLib : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Plugin NotificationLibrary is loaded!");
        }

        private void Init()
        {
            this.MainCamera = GameObject.Find("Main Camera");
            this.HUDObj = new GameObject();
            this.HUDObj2 = new GameObject();
            this.HUDObj2.name = "NOTIFICATIONLIB_HUD_OBJ";
            this.HUDObj.name = "NOTIFICATIONLIB_HUD_OBJ";
            this.HUDObj.AddComponent<Canvas>();
            this.HUDObj.AddComponent<CanvasScaler>();
            this.HUDObj.AddComponent<GraphicRaycaster>();
            this.HUDObj.GetComponent<Canvas>().enabled = true;
            this.HUDObj.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            this.HUDObj.GetComponent<Canvas>().worldCamera = this.MainCamera.GetComponent<Camera>();
            this.HUDObj.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 5f);
            this.HUDObj.GetComponent<RectTransform>().position = new Vector3(this.MainCamera.transform.position.x, this.MainCamera.transform.position.y, this.MainCamera.transform.position.z);
            this.HUDObj2.transform.position = new Vector3(this.MainCamera.transform.position.x, this.MainCamera.transform.position.y, this.MainCamera.transform.position.z - 4.6f);
            this.HUDObj.transform.parent = this.HUDObj2.transform;
            this.HUDObj.GetComponent<RectTransform>().localPosition = new Vector3(0f, 0f, 1.6f);
            Vector3 eulerAngles = this.HUDObj.GetComponent<RectTransform>().rotation.eulerAngles;
            eulerAngles.y = -270f;
            this.HUDObj.transform.localScale = new Vector3(1f, 1f, 1f);
            this.HUDObj.GetComponent<RectTransform>().rotation = Quaternion.Euler(eulerAngles);
            this.Testtext = new GameObject
            {
                transform =
                {
                    parent = this.HUDObj.transform
                }
            }.AddComponent<Text>();
            this.Testtext.text = "";
            this.Testtext.fontSize = 30;
            this.Testtext.font = currentFont;
            this.Testtext.rectTransform.sizeDelta = new Vector2(450f, 210f);
            this.Testtext.alignment = TextAnchor.LowerLeft;
            this.Testtext.rectTransform.localScale = new Vector3(0.00333333333f, 0.00333333333f, 0.33333333f);
            this.Testtext.rectTransform.localPosition = new Vector3(-1f, -1f, -0.5f);
            this.Testtext.material = this.AlertText;
            NotifiText = this.Testtext;
        }

        private void FixedUpdate()
        {
            bool flag = !this.HasInit && GameObject.Find("Main Camera") != null;
            if (flag)
            {
                this.Init();
                this.HasInit = true;
            }

            if (!this.HasInit) return;

            this.HUDObj2.transform.position = new Vector3(this.MainCamera.transform.position.x, this.MainCamera.transform.position.y, this.MainCamera.transform.position.z);
            this.HUDObj2.transform.rotation = this.MainCamera.transform.rotation;
            
            // Optimization: Avoid dynamic string splits and allocations on high-frequency update loops
            float currentTime = Time.time;
            bool changed = false;

            for (int i = 0; i < ActiveCount; i++)
            {
                if (currentTime >= ExpiryTimes[i])
                {
                    // Shift values inside arrays on notification expiry
                    for (int j = i; j < ActiveCount - 1; j++)
                    {
                        NotificationQueue[j] = NotificationQueue[j + 1];
                        ExpiryTimes[j] = ExpiryTimes[j + 1];
                    }
                    ActiveCount--;
                    NotificationQueue[ActiveCount] = null;
                    ExpiryTimes[ActiveCount] = 0f;
                    i--;
                    changed = true;
                }
            }

            if (changed || QueueDirty)
            {
                RebuildDisplayText();
                QueueDirty = false;
            }
        }

        private void RebuildDisplayText()
        {
            if (NotifiText == null) return;

            StringBuilderCache.Clear();
            for (int i = 0; i < ActiveCount; i++)
            {
                StringBuilderCache.Append(NotificationQueue[i]);
                StringBuilderCache.Append(Environment.NewLine);
            }
            NotifiText.text = StringBuilderCache.ToString();
        }

        public static void SendNotification(string NotificationText)
        {
            if (!disableNotifications && !string.IsNullOrEmpty(NotificationText))
            {
                try
                {
                    if (IsEnabled && PreviousNotifi != NotificationText)
                    {
                        // Clean trailing space requirements dynamically without causing GC thrashing
                        string formattedText = NotificationText;
                        if (!formattedText.EndsWith(Environment.NewLine))
                        {
                            formattedText += Environment.NewLine;
                        }

                        if (ActiveCount >= MaxStoredNotifications)
                        {
                            // Pop oldest notification from array
                            for (int i = 0; i < MaxStoredNotifications - 1; i++)
                            {
                                NotificationQueue[i] = NotificationQueue[i + 1];
                                ExpiryTimes[i] = ExpiryTimes[i + 1];
                            }
                            ActiveCount--;
                        }

                        NotificationQueue[ActiveCount] = formattedText;
                        ExpiryTimes[ActiveCount] = Time.time + 4.5f; // Hard limit lifespan duration
                        ActiveCount++;

                        PreviousNotifi = NotificationText;
                        QueueDirty = true;
                    }
                }
                catch
                {
                    Debug.LogError("Notification failed, object probably nil due to third person ; " + NotificationText);
                }
            }
        }

        public static void ClearAllNotifications()
        {
            //NotifiLib.NotifiText.text = "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> <color=white>Notifications cleared.</color>" + Environment.NewLine;
            
            for (int i = 0; i < MaxStoredNotifications; i++)
            {
                NotificationQueue[i] = null;
                ExpiryTimes[i] = 0f;
            }
            ActiveCount = 0;
            QueueDirty = true;
        }

        public static void ClearPastNotifications(int amount)
        {
            int clearAmount = Mathf.Min(amount, ActiveCount);
            if (clearAmount <= 0) return;

            for (int i = 0; i < ActiveCount - clearAmount; i++)
            {
                NotificationQueue[i] = NotificationQueue[i + clearAmount];
                ExpiryTimes[i] = ExpiryTimes[i + clearAmount];
            }

            for (int i = ActiveCount - clearAmount; i < MaxStoredNotifications; i++)
            {
                NotificationQueue[i] = null;
                ExpiryTimes[i] = 0f;
            }

            ActiveCount -= clearAmount;
            QueueDirty = true;
        }

        private GameObject HUDObj;

        private GameObject HUDObj2;

        private GameObject MainCamera;

        private Text Testtext;

        private Material AlertText = new Material(Shader.Find("GUI/Text Shader"));

        private int NotificationDecayTime = 144;

        private int NotificationDecayTimeCounter;

        public static int NoticationThreshold = 30;

        private string[] Notifilines;

        private string newtext;

        public static string PreviousNotifi;

        private bool HasInit;

        private static Text NotifiText;

        public static bool IsEnabled = true;

        // Structured buffer properties for zero-allocation performance safety
        private const int MaxStoredNotifications = 5;
        private static readonly string[] NotificationQueue = new string[MaxStoredNotifications];
        private static readonly float[] ExpiryTimes = new float[MaxStoredNotifications];
        private static int ActiveCount = 0;
        private static bool QueueDirty = false;
        private static readonly StringBuilder StringBuilderCache = new StringBuilder(512);
    }
}
