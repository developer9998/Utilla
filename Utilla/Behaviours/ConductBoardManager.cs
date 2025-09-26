using GorillaNetworking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Utilla.Tools;

namespace Utilla.Behaviours
{
    // TODO: expand range to support conduct board in virtual stump
    internal class ConductBoardManager : MonoBehaviour
    {
        private int PageCount => boardContent.Count;

        private readonly List<Section> boardContent = [];

        private ModeSelectButton buttonTemplate;

        private GameObject stumpRootObject;

        private Transform conductTransform;

        private TextMeshPro headingText, bodyText;

        private GameObject newSplash;

        private int currentPage = 0;

        public void Start()
        {
            GTZone startZone = PhotonNetworkController.Instance.StartZone;
            if (!UtillaGamemodeSelector.SelectorLookup.TryGetValue(startZone, out UtillaGamemodeSelector originalSelector)) return;

            buttonTemplate = originalSelector.Layout.pf_button;

            stumpRootObject = Array.Find(ZoneManagement.instance.GetZoneData(startZone).rootGameObjects, gameObject => gameObject.name == "TreeRoom");
            conductTransform = stumpRootObject.transform.FindChildRecursive("code of conduct");

            string codeOfConductHeading, codeOfConductBody;

            headingText = stumpRootObject.transform.FindChildRecursive("CodeOfConductHeadingText")?.GetComponent<TextMeshPro>();
            if (headingText != null)
            {
                headingText.fontSizeMax = headingText.fontSize;
                headingText.enableAutoSizing = true;
                headingText.textWrappingMode = TextWrappingModes.NoWrap;
                codeOfConductHeading = headingText.text;

                newSplash = Instantiate(buttonTemplate.newModeSplash, headingText.transform);
                newSplash.transform.localPosition = Vector3.down * 56.5f;
                newSplash.transform.localEulerAngles = Vector3.zero;
                newSplash.transform.localScale = Vector3.one * 19f;
                newSplash.SetActive(false);
            }
            else
            {
                codeOfConductHeading = "GORILLA CODE OF CONDUCT";
            }

            bodyText = stumpRootObject.transform.FindChildRecursive("COCBodyText")?.GetComponent<TextMeshPro>();
            if (bodyText != null)
            {
                bodyText.fontSizeMax = bodyText.fontSize;
                bodyText.fontSizeMin = 0f;
                bodyText.enableAutoSizing = true;
                bodyText.margin = new Vector4(0f, 0f, 0f, 36f);
                bodyText.richText = true;
                codeOfConductBody = bodyText.text;
            }
            else
            {
                StringBuilder str = new();
                str.AppendLine("- NO RACISM, SEXISM, HOMOPHOBIA, TRANSPHOBIA, OR OTHER BIGOTRY");
                str.AppendLine("- NO CHEATS OR MODS");
                str.AppendLine("- DO NOT HARASS OTHER PLAYERS OR INTENTIONALLY MAKE THEM UNCOMFORTABLE");
                str.AppendLine("- DO NOT TROLL OR GRIEF LOBBIES BY BEING UNCATCHABLE OR BY ESCAPING THE MAP. TRY TO MAKE SURE EVERYONE IS HAVING FUN");
                str.AppendLine("- IF SOMEONE IS BREAKING THIS CODE, PLEASE REPORT THEM");
                str.Append("- PLEASE BE NICE GORILLAS AND HAVE A GOOD TIME");
                codeOfConductBody = str.ToString();
                str = null;
            }

            boardContent.Insert(0, new()
            {
                Title = codeOfConductHeading,
                Body = codeOfConductBody
            });

            CreateButton(-1f, "-->", NextPage);
            CreateButton(1f, "<--", PrevPage);

            ShowPage();
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
            if (headingText == null || bodyText == null) return;

            Section content = boardContent.ElementAtOrDefault(Mathf.Max(0, Mathf.Min(currentPage, boardContent.Count - 1)));
            headingText.text = content.Title;
            bodyText.text = content.Body;
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

        public async void DownloadEntries()
        {
            string modDataLink = string.Concat(Constants.InfoRepositoryURL, "ModData.json");

            UnityWebRequest webRequest = UnityWebRequest.Get(modDataLink);
            UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
            await asyncOperation;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Logging.Fatal($"ModData could not be accessed from {modDataLink}");
                Logging.Info(webRequest.downloadHandler.error);
                return;
            }

            JObject jsonObject = JObject.Parse(webRequest.downloadHandler.text);
            JArray jsonArray = (JArray)jsonObject.Property("conductBoardSections").Value;

            foreach (JObject item in jsonArray.Cast<JObject>())
            {
                Logging.Message(item.ToString(Formatting.Indented));

                string title = (string)item.Property("title").Value;

                webRequest = UnityWebRequest.Get(string.Concat(Constants.InfoRepositoryURL, (string)item.Property("body").Value));
                asyncOperation = webRequest.SendWebRequest();
                await asyncOperation;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Logging.Fatal($"Body text could not be accessed from {webRequest.url}");
                    Logging.Error(webRequest.downloadHandler.error);
                    continue;
                }

                boardContent.Add(new()
                {
                    Title = title,
                    Body = webRequest.downloadHandler.text
                });
            }

            /*
            BoardSectionRequest[] sectionRequestArray = ().ToObject<BoardSectionRequest[]>();

            foreach (BoardSectionRequest request in sectionRequestArray)
            {
                string entryBodyLink = string.Concat(Constants.ModInfoRepository, request.body);

                webRequest = UnityWebRequest.Get(entryBodyLink);
                asyncOperation = webRequest.SendWebRequest();
                await asyncOperation;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Logging.Fatal($"Body text could not be accessed from {entryBodyLink}");
                    Logging.Error(webRequest.downloadHandler.error);
                    continue;
                }

                boardContent.Add(new()
                {
                    Heading = request.title,
                    Body = webRequest.downloadHandler.text
                });
            }
            */
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

        private struct Section
        {
            [TextArea(1, 1)]
            public string Title;

            [TextArea(12, 32)]
            public string Body;
        }
    }
}
