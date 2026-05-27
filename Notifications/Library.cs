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
        private const int MaxStoredNotifications = 5;
        private static readonly string[] NotificationQueue = new string[MaxStoredNotifications];
        private static readonly float[] ExpiryTimes = new float[MaxStoredNotifications];
        private static int ActiveCount = 0;
        private static bool QueueDirty = false;

        private GameObject _hudObj;
        private GameObject _hudObj2;
        private GameObject _mainCamera;
        private Text _testtext;
        private readonly Material _alertText = new Material(Shader.Find("GUI/Text Shader"));
        private readonly StringBuilder _stringBuilder = new StringBuilder(512);

        private bool _hasInit;

        private void Awake()
        {
            Logger.LogInfo("Zero-Allocation NotificationLibrary initialized.");
        }

        private void Init()
        {
            _mainCamera = GameObject.Find("Main Camera");
            if (_mainCamera == null) return;

            _hudObj = new GameObject("NOTIFICATIONLIB_HUD_OBJ");
            _hudObj2 = new GameObject("NOTIFICATIONLIB_HUD_OBJ2");

            _hudObj.AddComponent<Canvas>();
            _hudObj.AddComponent<CanvasScaler>();
            _hudObj.AddComponent<GraphicRaycaster>();

            Canvas canvas = _hudObj.GetComponent<Canvas>();
            canvas.enabled = true;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera.GetComponent<Camera>();

            RectTransform rect = _hudObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(5f, 5f);
            rect.position = _mainCamera.transform.position;

            _hudObj2.transform.position = _mainCamera.transform.position - new Vector3(0, 0, 4.6f);
            _hudObj.transform.parent = _hudObj2.transform;
            rect.localPosition = new Vector3(0f, 0f, 1.6f);

            Vector3 eulerAngles = rect.rotation.eulerAngles;
            eulerAngles.y = -270f;
            _hudObj.transform.localScale = Vector3.one;
            rect.rotation = Quaternion.Euler(eulerAngles);

            GameObject textGo = new GameObject("NotificationText");
            textGo.transform.SetParent(_hudObj.transform, false);

            _testtext = textGo.AddComponent<Text>();
            _testtext.text = string.Empty;
            _testtext.fontSize = 30;
            _testtext.font = currentFont;
            _testtext.rectTransform.sizeDelta = new Vector2(450f, 210f);
            _testtext.alignment = TextAnchor.LowerLeft;
            _testtext.rectTransform.localScale = new Vector3(0.00333333333f, 0.00333333333f, 0.33333333f);
            _testtext.rectTransform.localPosition = new Vector3(-1f, -1f, -0.5f);
            _testtext.material = _alertText;

            _hasInit = true;
        }

        private void FixedUpdate()
        {
            if (!_hasInit)
            {
                Init();
                return;
            }

            if (_mainCamera == null) return;

            Transform camTrans = _mainCamera.transform;
            _hudObj2.transform.SetPositionAndRotation(camTrans.position, camTrans.rotation);

            float currentTime = Time.time;
            bool changed = false;

            for (int i = 0; i < ActiveCount; i++)
            {
                if (currentTime >= ExpiryTimes[i])
                {
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
                UpdateDisplayText();
                QueueDirty = false;
            }
        }

        private void UpdateDisplayText()
        {
            if (_testtext == null) return;

            _stringBuilder.Clear();
            for (int i = 0; i < ActiveCount; i++)
            {
                _stringBuilder.Append(NotificationQueue[i]);
                _stringBuilder.Append(Environment.NewLine);
            }
            _testtext.text = _stringBuilder.ToString();
        }

        public static void SendNotification(string notificationText)
        {
            if (disableNotifications || string.IsNullOrEmpty(notificationText)) return;

            if (ActiveCount >= MaxStoredNotifications)
            {
                for (int i = 0; i < MaxStoredNotifications - 1; i++)
                {
                    NotificationQueue[i] = NotificationQueue[i + 1];
                    ExpiryTimes[i] = ExpiryTimes[i + 1];
                }
                ActiveCount--;
            }

            NotificationQueue[ActiveCount] = notificationText;
            ExpiryTimes[ActiveCount] = Time.time + 4.5f; 
            ActiveCount++;
            QueueDirty = true;
        }

        public static void ClearAllNotifications()
        {
            for (int i = 0; i < MaxStoredNotifications; i++)
            {
                NotificationQueue[i] = null;
                ExpiryTimes[i] = 0f;
            }
            ActiveCount = 0;
            QueueDirty = true;
        }
    }
}
