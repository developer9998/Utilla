using GorillaNetworking;
using GorillaTag;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Utilla.Tools;

namespace Utilla.Behaviours
{
    internal class ConductBoardManager : MonoBehaviour
    {
        private int PageCount => boardContent.Count;

        private readonly List<Section> boardContent = [];

        private ModeSelectButton buttonTemplate;

        private GameObject stumpRootObject;

        private Transform conductTransform;

        private TextMeshPro baseHeaderText, baseBodyText, headerText, bodyText, footerText;

        private int currentPage = 0;

        public void Start()
        {
            GTZone startZone = PhotonNetworkController.Instance.StartZone;
            if (!UtillaGamemodeSelector.SelectorLookup.TryGetValue(startZone, out UtillaGamemodeSelector originalSelector)) return;

            buttonTemplate = originalSelector.Layout.pf_button;

            stumpRootObject = Array.Find(ZoneManagement.instance.GetZoneData(startZone).rootGameObjects, gameObject => gameObject.name == "TreeRoom");
            conductTransform = stumpRootObject.transform.FindChildRecursive("code of conduct");

            baseHeaderText = stumpRootObject.transform.FindChildRecursive("CodeOfConductHeadingText")?.GetComponent<TextMeshPro>();
            if (baseHeaderText == null)
            {
                Logging.Warning("COC (Code of Conduct) header text is missing");
                return;
            }

            GameObject headingTextObject = Instantiate(baseHeaderText.gameObject);
            headingTextObject.transform.position = baseHeaderText.transform.position;
            headingTextObject.transform.rotation = baseHeaderText.transform.rotation;
            headingTextObject.transform.localScale = baseHeaderText.transform.localScale;
            SanitizeTextObject(headingTextObject);

            headerText = headingTextObject.GetComponent<TextMeshPro>();
            headerText.fontSizeMax = baseHeaderText.fontSize;
            headerText.enableAutoSizing = true;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;

            GameObject footerTextObject = Instantiate(baseHeaderText.gameObject);
            footerTextObject.transform.position = baseHeaderText.transform.position;
            footerTextObject.transform.rotation = baseHeaderText.transform.rotation;
            footerTextObject.transform.localScale = baseHeaderText.transform.localScale;
            SanitizeTextObject(footerTextObject);

            footerText = footerTextObject.GetComponent<TextMeshPro>();
            footerText.text = $"{Constants.Name} {Constants.Version}".ToUpper();
            footerText.enableAutoSizing = false;
            footerText.fontSize = 45;
            footerText.margin = new Vector4(0f, 110f, 0f, 0f);
            footerText.characterSpacing = -11.5f;
            footerText.enabled = true;
            footerText.renderer.enabled = true;

            baseBodyText = stumpRootObject.transform.FindChildRecursive("COCBodyText")?.GetComponent<TextMeshPro>();
            if (baseBodyText == null)
            {
                Logging.Warning("COC (Code of Conduct) body text is missing");
                return;
            }

            GameObject bodyTextObject = Instantiate(baseBodyText.gameObject);
            bodyTextObject.transform.position = baseBodyText.transform.position;
            bodyTextObject.transform.rotation = baseBodyText.transform.rotation;
            bodyTextObject.transform.localScale = baseBodyText.transform.localScale;
            SanitizeTextObject(bodyTextObject);

            bodyText = bodyTextObject.GetComponent<TextMeshPro>();
            bodyText.fontSizeMax = baseBodyText.fontSize;
            bodyText.fontSizeMin = 0f;
            bodyText.enableAutoSizing = true;
            bodyText.margin = new Vector4(0f, 0f, 0f, 36f);
            bodyText.richText = true;

            boardContent.Insert(0, new());

            CreateButton(-1f, "-->", NextPage);
            CreateButton(1f, "<--", PrevPage);

            ShowPage();
            CheckVersion();
            DownloadEntries();
        }

        private void NextPage()
        {
            currentPage = (currentPage + 1) % PageCount;
            ShowPage();
        }

        private void PrevPage()
        {
            currentPage = (currentPage <= 0) ? PageCount - 1 : currentPage - 1;
            ShowPage();
        }

        private void ShowPage()
        {
            if (baseHeaderText == null || baseBodyText == null) return;

            Section content = boardContent.ElementAtOrDefault(Mathf.Max(0, Mathf.Min(currentPage, boardContent.Count - 1)));

            if (content.UseBaseText)
            {
                baseHeaderText.enabled = true;
                headerText.enabled = false;

                baseBodyText.enabled = true;
                bodyText.enabled = false;
            }
            else
            {
                baseHeaderText.enabled = false;
                headerText.enabled = true;
                headerText.text = content.Title;

                baseBodyText.enabled = false;
                bodyText.enabled = true;
                bodyText.text = content.Body;
            }
        }

        private void CreateButton(float horizontalPosition, string text, Action onButtonPressed = null)
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonObject.transform.parent = conductTransform;
            buttonObject.transform.localPosition = new Vector3(horizontalPosition, 0.52f, 0.13f);
            buttonObject.transform.localRotation = Quaternion.Euler(353.5f, 0f, 0f);
            buttonObject.transform.localScale = new Vector3(0.1427168f, 0.1427168f, 0.1f);
            buttonObject.GetComponent<Renderer>().material = buttonTemplate.unpressedMaterial;
            buttonObject.GetComponent<Collider>().isTrigger = true;
            buttonObject.SetLayer(UnityLayer.GorillaInteractable);

            GameObject textObject = new();
            textObject.transform.parent = buttonObject.transform;
            textObject.transform.localPosition = Vector3.forward * 0.525f;
            textObject.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
            textObject.transform.localScale = Vector3.one;

            TextMeshPro textMeshPro = textObject.AddComponent<TextMeshPro>();
            textMeshPro.font = buttonTemplate?.GetComponentInChildren<TMP_Text>()?.font ?? stumpRootObject.GetComponentInChildren<GorillaComputerTerminal>()?.myScreenText?.font;
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.characterSpacing = -10f;
            textMeshPro.overflowMode = TextOverflowModes.Overflow;
            textMeshPro.fontSize = 3f;
            textMeshPro.color = new Color(0.1960784f, 0.1960784f, 0.1960784f);
            textMeshPro.text = text;

            GorillaPressableButton pressableButton = buttonObject.AddComponent<GorillaPressableButton>();
            pressableButton.buttonRenderer = buttonObject.GetComponent<MeshRenderer>();
            pressableButton.unpressedMaterial = buttonTemplate.unpressedMaterial;
            pressableButton.pressedMaterial = buttonTemplate.pressedMaterial;

            UnityEvent onPressEvent = new();
            onPressEvent.AddListener(new UnityAction(() =>
            {
                pressableButton.StartCoroutine(ButtonColourUpdate(pressableButton));
            }));
            onPressEvent.AddListener(new UnityAction(onButtonPressed));
            pressableButton.onPressButton = onPressEvent;
        }

        public async void CheckVersion()
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(string.Join('/', Constants.InfoRepositoryURL, "Version.txt"));
            UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
            await asyncOperation;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Logging.Fatal($"Version could not be accessed from {webRequest.url}");
                Logging.Info(webRequest.downloadHandler.error);
                return;
            }

            if (Version.TryParse(Constants.Version, out Version installedVersion) && Version.TryParse(webRequest.downloadHandler.text, out Version latestVersion) && latestVersion > installedVersion)
            {
                footerText.color = Color.red;
                footerText.fontSize *= 0.85f;
                footerText.text = $"{Constants.Name} {Constants.Version} - please update to {latestVersion}".ToUpper();
            }
        }

        public async void DownloadEntries()
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(string.Join('/', Constants.InfoRepositoryURL, "ConductBoard", "Entries.json"));
            UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
            await asyncOperation;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Logging.Fatal($"ModData could not be accessed from {webRequest.url}");
                Logging.Info(webRequest.downloadHandler.error);
                return;
            }

            foreach (JObject item in JArray.Parse(webRequest.downloadHandler.text).Cast<JObject>())
            {
                Logging.Message(item.ToString(Formatting.Indented));

                webRequest = UnityWebRequest.Get(string.Join('/', Constants.InfoRepositoryURL, (string)item.Property("body").Value));
                asyncOperation = webRequest.SendWebRequest();
                await asyncOperation;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Logging.Fatal($"Body text could not be accessed from {webRequest.url}");
                    Logging.Error(webRequest.downloadHandler.error);
                    continue;
                }

                boardContent.Add(new((string)item.Property("title").Value, webRequest.downloadHandler.text));
            }
        }

        private IEnumerator ButtonColourUpdate(GorillaPressableButton pressableButton)
        {
            pressableButton.isOn = true;
            pressableButton.UpdateColor();

            yield return new WaitForSeconds(pressableButton.debounceTime);
            if ((pressableButton.touchTime + pressableButton.debounceTime) < Time.time)
            {
                pressableButton.isOn = false;
                pressableButton.UpdateColor();
            }

            yield break;
        }

        private void SanitizeTextObject(GameObject gameObject)
        {
            Type[] typesToRemove = [typeof(PlayFabTitleDataTextDisplay), typeof(LocalizedText), typeof(StaticLodGroup)];
            Component[] components = gameObject.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Type type = components[i].GetType();
                if (typesToRemove.Contains(type)) Destroy(components[i]);
            }
        }

        private struct Section
        {
            public bool UseBaseText;

            [TextArea(1, 1)]
            public string Title;

            [TextArea(12, 32)]
            public string Body;

            public Section()
            {
                UseBaseText = true;
            }

            public Section(string title, string body)
            {
                UseBaseText = false;
                Title = title;
                Body = body;
            }
        }
    }
}
