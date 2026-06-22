using System.IO;
using Team3Project.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.EditorTools
{
    public static class DialogueUiSceneBuilder
    {
        private const string DialogueUiPrefabPath = "Assets/Resources/DialogueUi.prefab";

        [MenuItem("Tools/Team3/Create Or Update Dialogue UI Prefab")]
        public static void CreateOrUpdateDialogueUiPrefab()
        {
            Directory.CreateDirectory("Assets/Resources");

            var root = CreateDialogueRoot();
            PrefabUtility.SaveAsPrefabAsset(root, DialogueUiPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUiPrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        [MenuItem("Tools/Team3/Select Dialogue UI Prefab")]
        public static void SelectDialogueUiPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUiPrefabPath);
            if (prefab == null)
            {
                CreateOrUpdateDialogueUiPrefab();
                return;
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        [MenuItem("Tools/Team3/Create Editable Dialogue UI In Current Scene")]
        public static void CreateEditableDialogueUi()
        {
            var canvas = GameObject.Find("Dialogue Canvas");
            if (canvas == null)
            {
                canvas = new GameObject("Dialogue Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            EnsureCanvas(canvas);

            if (canvas.GetComponent<DialogueManager>() == null)
            {
                canvas.AddComponent<DialogueManager>();
            }

            var existingRoot = canvas.transform.Find("Dialogue Root");
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot.gameObject);
            }

            var root = CreateDialogueRoot();
            root.transform.SetParent(canvas.transform, false);
            root.SetActive(false);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(canvas.scene);
        }

        private static GameObject CreateDialogueRoot()
        {
            var root = new GameObject("Dialogue Root", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.35f);
            rootImage.raycastTarget = true;

            var panel = FindOrCreateChild(root.transform, "Dialogue Panel", typeof(Image));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(1500f, 300f);
            panelRect.anchoredPosition = new Vector2(0f, 36f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = LoadDialogueWindowSprite();
            panelImage.color = Color.white;

            CreateImage(panel.transform, "Character Image", new Vector2(-610f, 66f), new Vector2(170f, 170f));
            CreateText(panel.transform, "Character Name Text", new Vector2(-365f, 205f), new Vector2(760f, 48f), 30, TextAnchor.MiddleLeft);
            CreateText(panel.transform, "Dialogue Text", new Vector2(120f, 112f), new Vector2(1080f, 130f), 30, TextAnchor.UpperLeft);

            var nextButton = FindOrCreateChild(panel.transform, "Dialogue Next Button", typeof(Image), typeof(Button));
            var nextRect = nextButton.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1f, 0f);
            nextRect.anchorMax = new Vector2(1f, 0f);
            nextRect.pivot = new Vector2(1f, 0f);
            nextRect.sizeDelta = new Vector2(150f, 48f);
            nextRect.anchoredPosition = new Vector2(-32f, 24f);
            nextButton.GetComponent<Image>().color = new Color(0.86f, 0.75f, 0.55f, 1f);
            CreateText(nextButton.transform, "Label", Vector2.zero, new Vector2(140f, 42f), 24, TextAnchor.MiddleCenter).text = "\uB2E4\uC74C";

            return root;
        }

        private static void EnsureCanvas(GameObject canvas)
        {
            if (canvas.GetComponent<RectTransform>() == null)
            {
                canvas.AddComponent<RectTransform>();
            }

            if (canvas.GetComponent<Canvas>() == null)
            {
                canvas.AddComponent<Canvas>();
            }

            if (canvas.GetComponent<CanvasScaler>() == null)
            {
                canvas.AddComponent<CanvasScaler>();
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.AddComponent<GraphicRaycaster>();
            }

            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 500;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static Sprite LoadDialogueWindowSprite()
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resource/Dialogue" });
            if (guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject FindOrCreateChild(Transform parent, string objectName, params System.Type[] components)
        {
            var child = parent.Find(objectName);
            if (child != null)
            {
                EnsureComponents(child.gameObject, components);
                return child.gameObject;
            }

            var objectComponents = new System.Type[components.Length + 2];
            objectComponents[0] = typeof(RectTransform);
            objectComponents[1] = typeof(CanvasRenderer);
            for (var i = 0; i < components.Length; i++)
            {
                objectComponents[i + 2] = components[i];
            }

            var childObject = new GameObject(objectName, objectComponents);
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static void EnsureComponents(GameObject target, System.Type[] components)
        {
            if (target.GetComponent<RectTransform>() == null)
            {
                target.AddComponent<RectTransform>();
            }

            if (target.GetComponent<CanvasRenderer>() == null)
            {
                target.AddComponent<CanvasRenderer>();
            }

            foreach (var component in components)
            {
                if (target.GetComponent(component) == null)
                {
                    target.AddComponent(component);
                }
            }
        }

        private static Image CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size)
        {
            var imageObject = FindOrCreateChild(parent, objectName, typeof(Image));
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
            var textObject = FindOrCreateChild(parent, objectName, typeof(Text));
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
