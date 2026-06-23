using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team3Project.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        private const string DialogueCanvasName = "Dialogue Canvas";
        private const string DialogueUiPrefabPath = "DialogueUi";
        private static DialogueManager instance;

        [Header("UI Elements")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image characterImage;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Button nextButton;

        [Header("Defaults")]
        [SerializeField] private Sprite defaultCharacterImage;

        [Header("Typing Effect")]
        [SerializeField] private float typingSpeed = 0.035f;
        [SerializeField] private bool skipTypingOnClick = true;

        private DialogueDataSO currentDialogue;
        private int currentLineIndex;
        private bool isDialogueActive;
        private bool isTyping;
        private Coroutine typingCoroutine;
        private Action onComplete;
        private int lastAdvanceFrame = -1;
        private bool usesSceneUi;

        public static DialogueManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var managerObject = new GameObject("Dialogue Manager");
                instance = managerObject.AddComponent<DialogueManager>();
                return instance;
            }
        }

        public static void PlayResource(string resourcePath, Action onComplete = null)
        {
            var dialogue = Resources.Load<DialogueDataSO>(resourcePath);
            if (dialogue == null)
            {
                dialogue = Resources.Load<DialogueDataSO>("Dialogues/Default");
            }

            Instance.StartDialogue(dialogue, onComplete);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureUi();
            if (!usesSceneUi)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (!isDialogueActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.S))
            {
                SkipDialogue();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || HasTouchBegan())
            {
                HandleNextInput();
            }
        }

        public bool IsDialogueActive()
        {
            return isDialogueActive;
        }

        public static bool HasActiveDialogue()
        {
            return instance != null && instance.isDialogueActive;
        }

        public void StartDialogue(DialogueDataSO dialogue, Action completeCallback = null)
        {
            EnsureUi();
            if (dialogue == null || dialogue.LineCount == 0)
            {
                completeCallback?.Invoke();
                return;
            }

            currentDialogue = dialogue;
            currentLineIndex = 0;
            isDialogueActive = true;
            onComplete = completeCallback;

            dialogueRoot.SetActive(true);
            dialoguePanel.SetActive(true);
            ApplyDefaultBackground();

            ShowCurrentLine();
        }

        public void HandleNextInput()
        {
            if (!isDialogueActive || lastAdvanceFrame == Time.frameCount)
            {
                return;
            }

            lastAdvanceFrame = Time.frameCount;
            if (isTyping && skipTypingOnClick)
            {
                CompleteTyping();
                return;
            }

            if (!isTyping)
            {
                ShowNextLine();
            }
        }

        public void SkipDialogue()
        {
            EndDialogue();
        }

        private void ShowCurrentLine()
        {
            if (currentDialogue == null || currentLineIndex >= currentDialogue.LineCount)
            {
                EndDialogue();
                return;
            }

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            ApplyCurrentLinePresentation();
            typingCoroutine = StartCoroutine(TypeText(GetCurrentLineText()));
        }

        private void ShowNextLine()
        {
            currentLineIndex++;
            if (currentDialogue == null || currentLineIndex >= currentDialogue.LineCount)
            {
                EndDialogue();
                return;
            }

            ShowCurrentLine();
        }

        private IEnumerator TypeText(string textToType)
        {
            isTyping = true;
            dialogueText.text = string.Empty;

            for (var i = 0; i < textToType.Length; i++)
            {
                dialogueText.text += textToType[i];
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
        }

        private void CompleteTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            isTyping = false;
            if (currentDialogue != null && currentLineIndex < currentDialogue.LineCount)
            {
                dialogueText.text = GetCurrentLineText();
            }
        }

        private string GetCurrentLineText()
        {
            return currentDialogue?.GetLine(currentLineIndex)?.text ?? string.Empty;
        }

        private void ApplyCurrentLinePresentation()
        {
            if (currentDialogue == null)
            {
                return;
            }

            var line = currentDialogue.GetLine(currentLineIndex);
            if (line == null)
            {
                return;
            }

            if (characterNameText != null)
            {
                characterNameText.text = currentDialogue.UsesEntries
                    ? line.characterName
                    : string.IsNullOrWhiteSpace(line.characterName) ? currentDialogue.characterName : line.characterName;
            }

            if (characterImage != null)
            {
                var sprite = line.characterImage != null ? line.characterImage : null;
                if (sprite == null && !currentDialogue.UsesEntries)
                {
                    sprite = currentDialogue.characterImage != null ? currentDialogue.characterImage : defaultCharacterImage;
                }

                characterImage.sprite = sprite;
                characterImage.gameObject.SetActive(sprite != null);
                characterImage.preserveAspect = true;
            }

            if (line.changeBackground)
            {
                ApplyBackground(line.backgroundImage, line.backgroundColor);
            }
        }

        private void ApplyDefaultBackground()
        {
            if (currentDialogue == null)
            {
                return;
            }

            if (currentDialogue.useDefaultBackground)
            {
                ApplyBackground(currentDialogue.defaultBackgroundImage, currentDialogue.defaultBackgroundColor);
            }
        }

        private void ApplyBackground(Sprite sprite, Color color)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = sprite;
            backgroundImage.color = sprite == null ? color : Color.white;
            backgroundImage.preserveAspect = false;
        }

        private void EndDialogue()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            isDialogueActive = false;
            isTyping = false;
            currentLineIndex = 0;
            dialoguePanel.SetActive(false);
            dialogueRoot.SetActive(false);

            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private void EnsureUi()
        {
            if (HasRequiredUi())
            {
                return;
            }

            var canvasObject = GameObject.Find(DialogueCanvasName);
            if (canvasObject == null)
            {
                canvasObject = new GameObject(DialogueCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                DontDestroyOnLoad(canvasObject);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (TryBindSceneUi())
            {
                usesSceneUi = true;
                dialogueRoot.SetActive(false);
                return;
            }

            if (TryLoadPrefabUi(canvasObject.transform))
            {
                usesSceneUi = false;
                dialogueRoot.SetActive(false);
                return;
            }

            usesSceneUi = false;
            dialogueRoot = new GameObject("Dialogue Root", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dialogueRoot.transform.SetParent(canvasObject.transform, false);
            var rootRect = dialogueRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var rootImage = dialogueRoot.GetComponent<Image>();
            backgroundImage = rootImage;
            rootImage.color = new Color(0f, 0f, 0f, 0.35f);
            rootImage.raycastTarget = true;

            dialoguePanel = new GameObject("Dialogue Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dialoguePanel.transform.SetParent(dialogueRoot.transform, false);
            var panelRect = dialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(1500f, 300f);
            panelRect.anchoredPosition = new Vector2(0f, 36f);

            var panelImage = dialoguePanel.GetComponent<Image>();
            panelImage.sprite = LoadDialogueWindowSprite();
            panelImage.color = panelImage.sprite == null ? new Color(0.12f, 0.09f, 0.08f, 0.92f) : Color.white;

            characterImage = CreateImage(dialoguePanel.transform, "Character Image", new Vector2(-610f, 66f), new Vector2(170f, 170f));
            characterNameText = CreateText(dialoguePanel.transform, "Character Name Text", new Vector2(-365f, 205f), new Vector2(760f, 48f), 30, TextAnchor.MiddleLeft);
            dialogueText = CreateText(dialoguePanel.transform, "Dialogue Text", new Vector2(120f, 112f), new Vector2(1080f, 130f), 30, TextAnchor.UpperLeft);

            nextButton = new GameObject("Dialogue Next Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            nextButton.transform.SetParent(dialoguePanel.transform, false);
            var nextRect = nextButton.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1f, 0f);
            nextRect.anchorMax = new Vector2(1f, 0f);
            nextRect.pivot = new Vector2(1f, 0f);
            nextRect.sizeDelta = new Vector2(150f, 48f);
            nextRect.anchoredPosition = new Vector2(-32f, 24f);
            nextButton.GetComponent<Image>().color = new Color(0.86f, 0.75f, 0.55f, 1f);
            CreateText(nextButton.transform, "Label", Vector2.zero, new Vector2(140f, 42f), 24, TextAnchor.MiddleCenter).text = "\uB2E4\uC74C";

            dialogueRoot.SetActive(false);
        }

        private bool HasRequiredUi()
        {
            return dialogueRoot != null
                && dialoguePanel != null
                && backgroundImage != null
                && characterNameText != null
                && dialogueText != null;
        }

        private bool TryLoadPrefabUi(Transform canvasTransform)
        {
            var prefab = Resources.Load<GameObject>(DialogueUiPrefabPath);
            if (prefab == null)
            {
                return false;
            }

            dialogueRoot = Instantiate(prefab, canvasTransform, false);
            dialogueRoot.name = "Dialogue Root";
            return TryBindFromRoot(dialogueRoot);
        }

        private bool TryBindSceneUi()
        {
            dialogueRoot = dialogueRoot != null ? dialogueRoot : FindSceneObject("Dialogue Root");
            return TryBindFromRoot(dialogueRoot);
        }

        private bool TryBindFromRoot(GameObject root)
        {
            if (root == null)
            {
                ResetUiReferences();
                return false;
            }

            dialogueRoot = root;
            backgroundImage = backgroundImage != null && backgroundImage.transform.IsChildOf(root.transform)
                ? backgroundImage
                : root.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = root.AddComponent<Image>();
                backgroundImage.raycastTarget = true;
            }

            dialoguePanel = dialoguePanel != null ? dialoguePanel : FindSceneObject("Dialogue Panel");
            if (dialoguePanel == null || !dialoguePanel.transform.IsChildOf(root.transform))
            {
                dialoguePanel = FindChild(root.transform, "Dialogue Panel");
            }

            characterImage = characterImage != null && characterImage.transform.IsChildOf(root.transform)
                ? characterImage
                : FindChildComponent<Image>(root.transform, "Character Image");
            characterNameText = characterNameText != null && characterNameText.transform.IsChildOf(root.transform)
                ? characterNameText
                : FindChildComponent<Text>(root.transform, "Character Name Text");
            dialogueText = dialogueText != null && dialogueText.transform.IsChildOf(root.transform)
                ? dialogueText
                : FindChildComponent<Text>(root.transform, "Dialogue Text");
            nextButton = nextButton != null && nextButton.transform.IsChildOf(root.transform)
                ? nextButton
                : FindChildComponent<Button>(root.transform, "Dialogue Next Button");

            if (dialogueRoot == null || dialoguePanel == null || characterNameText == null || dialogueText == null)
            {
                Debug.LogWarning("Dialogue UI was not found in this scene. Falling back to runtime generated dialogue UI.");
                ResetUiReferences();
                return false;
            }

            return true;
        }

        private void ResetUiReferences()
        {
            dialogueRoot = null;
            dialoguePanel = null;
            backgroundImage = null;
            characterImage = null;
            characterNameText = null;
            dialogueText = null;
            nextButton = null;
        }

        private static GameObject FindChild(Transform root, string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static T FindChildComponent<T>(Transform root, string objectName) where T : Component
        {
            var target = FindChild(root, objectName);
            return target == null ? null : target.GetComponent<T>();
        }

        private static GameObject FindSceneObject(string objectName)
        {
            var activeScene = SceneManager.GetActiveScene();
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var target in objects)
            {
                if (target.name == objectName && target.scene == activeScene)
                {
                    return target;
                }
            }

            foreach (var target in objects)
            {
                if (target.name == objectName && target.scene.IsValid())
                {
                    return target;
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(string objectName) where T : Component
        {
            var target = FindSceneObject(objectName);
            return target == null ? null : target.GetComponent<T>();
        }

        private static bool HasTouchBegan()
        {
            if (Input.touchCount <= 0)
            {
                return false;
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    return true;
                }
            }

            return false;
        }

        private static Sprite LoadDialogueWindowSprite()
        {
            var directoryPath = Path.Combine(Application.dataPath, "Resource", "Dialogue");
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"Dialogue sprite folder not found: {directoryPath}");
                return null;
            }

            var files = Directory.GetFiles(directoryPath, "*.png");
            if (files.Length == 0)
            {
                Debug.LogWarning($"Dialogue sprite not found: {directoryPath}");
                return null;
            }

            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(files[0])))
            {
                Debug.LogWarning($"Failed to load dialogue sprite: {files[0]}");
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(files[0]);
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Image CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return imageObject.GetComponent<Image>();
        }

        private static Text CreateText(Transform parent, string objectName, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
