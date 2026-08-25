using Pokemon.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pokemon.EditorTools
{
    public static class DialogueSceneHierarchyBuilder
    {
        private const string MenuPath = "Tools/Pokemon/场景/创建对话系统层级";

        /// <summary>
        /// 在当前选中的 Canvas 下创建对话面板、交互提示和对话控制对象。
        /// </summary>
        [MenuItem(MenuPath)]
        private static void CreateDialogueHierarchy()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "World")
            {
                EditorUtility.DisplayDialog("创建对话系统层级", "请先打开 World 场景。", "确定");
                return;
            }

            Canvas canvas = GetSelectedCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "创建对话系统层级",
                    "请在 Hierarchy 中选中 WorldCanvas，或选中 WorldCanvas 下的任意对象。",
                    "确定");
                return;
            }

            if (GetDirectChild(canvas.transform, "DialogueLayer") != null ||
                GetRootObject(scene, "DialogueSystem") != null)
            {
                EditorUtility.DisplayDialog(
                    "创建对话系统层级",
                    "场景中已经存在 DialogueLayer 或 DialogueSystem，请先检查现有层级。",
                    "确定");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建对话系统层级");

            GameObject dialogueLayer = CreateUiObject("DialogueLayer", canvas.transform);
            SetStretch(dialogueLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            dialogueLayer.transform.SetAsLastSibling();

            GameObject dialoguePanel = CreatePanel("DialoguePanel", dialogueLayer.transform);
            SetStretch(
                dialoguePanel.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.04f),
                new Vector2(0.94f, 0.31f));

            GameObject leftPortraitRoot = CreateUiObject("LeftPortraitRoot", dialoguePanel.transform);
            SetStretch(
                leftPortraitRoot.GetComponent<RectTransform>(),
                new Vector2(0.015f, 0.06f),
                new Vector2(0.22f, 0.94f));
            CreatePortraitImage("PortraitImage", leftPortraitRoot.transform);

            GameObject rightPortraitRoot = CreateUiObject("RightPortraitRoot", dialoguePanel.transform);
            SetStretch(
                rightPortraitRoot.GetComponent<RectTransform>(),
                new Vector2(0.78f, 0.06f),
                new Vector2(0.985f, 0.94f));
            CreatePortraitImage("PortraitImage", rightPortraitRoot.transform);

            GameObject textArea = CreateUiObject("TextArea", dialoguePanel.transform);
            SetStretch(
                textArea.GetComponent<RectTransform>(),
                new Vector2(0.235f, 0.08f),
                new Vector2(0.765f, 0.92f));

            GameObject speakerNameText = CreateText("SpeakerNameText", textArea.transform, 30f);
            SetStretch(
                speakerNameText.GetComponent<RectTransform>(),
                new Vector2(0f, 0.76f),
                new Vector2(1f, 1f));
            speakerNameText.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

            GameObject dialogueText = CreateText("DialogueText", textArea.transform, 27f);
            SetStretch(
                dialogueText.GetComponent<RectTransform>(),
                new Vector2(0f, 0.16f),
                new Vector2(1f, 0.76f));

            GameObject continueIndicator = CreateText("ContinueIndicator", textArea.transform, 24f);
            SetStretch(
                continueIndicator.GetComponent<RectTransform>(),
                new Vector2(0.9f, 0f),
                new Vector2(1f, 0.16f));
            TMP_Text continueText = continueIndicator.GetComponent<TMP_Text>();
            continueText.text = ">";
            continueText.alignment = TextAlignmentOptions.Center;

            GameObject interactionPrompt = CreatePanel("InteractionPrompt", dialogueLayer.transform);
            RectTransform promptRect = interactionPrompt.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.64f);
            promptRect.anchorMax = new Vector2(0.5f, 0.64f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = Vector2.zero;
            promptRect.sizeDelta = new Vector2(420f, 64f);

            GameObject promptText = CreateText("PromptText", interactionPrompt.transform, 24f);
            SetStretch(promptText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 12f);
            TMP_Text promptLabel = promptText.GetComponent<TMP_Text>();
            promptLabel.text = "按 E 对话";
            promptLabel.alignment = TextAlignmentOptions.Center;

            GameObject dialogueSystem = new GameObject("DialogueSystem");
            Undo.RegisterCreatedObjectUndo(dialogueSystem, "创建 DialogueSystem");
            dialogueSystem.AddComponent<DialogueController>();
            AudioSource audioSource = dialogueSystem.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            dialoguePanel.SetActive(false);
            interactionPrompt.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = dialogueSystem;

            EditorUtility.DisplayDialog(
                "创建对话系统层级",
                "对话层级已创建。请保存 World 场景，并按使用说明完成 Inspector 引用绑定。",
                "确定");
        }

        /// <summary>
        /// 仅在当前选中对象包含 Canvas 组件时启用创建菜单。
        /// </summary>
        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateDialogueHierarchy()
        {
            return SceneManager.GetActiveScene().name == "World";
        }

        /// <summary>
        /// 返回当前选中对象自身或父级中的 Canvas 组件。
        /// </summary>
        private static Canvas GetSelectedCanvas()
        {
            if (Selection.activeGameObject == null)
                return null;

            Canvas canvas = Selection.activeGameObject.GetComponent<Canvas>();
            return canvas != null
                ? canvas
                : Selection.activeGameObject.GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// 创建带 RectTransform 的 UI 对象并设置父级和画布层级。
        /// </summary>
        /// <param name="name">新对象名称。</param>
        /// <param name="parent">新对象的父级 Transform。</param>
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, $"创建 {name}");
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        /// <summary>
        /// 创建带半透明背景的 UI 面板。
        /// </summary>
        /// <param name="name">面板对象名称。</param>
        /// <param name="parent">面板父级 Transform。</param>
        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = CreateUiObject(name, parent);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.055f, 0.075f, 0.1f, 0.94f);
            return panel;
        }

        /// <summary>
        /// 创建铺满父对象且保持原始比例的立绘 Image。
        /// </summary>
        /// <param name="name">立绘对象名称。</param>
        /// <param name="parent">立绘父级 Transform。</param>
        private static GameObject CreatePortraitImage(string name, Transform parent)
        {
            GameObject portrait = CreateUiObject(name, parent);
            SetStretch(portrait.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Image image = portrait.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return portrait;
        }

        /// <summary>
        /// 创建用于对话界面的 TextMeshPro 文本对象。
        /// </summary>
        /// <param name="name">文本对象名称。</param>
        /// <param name="parent">文本父级 Transform。</param>
        /// <param name="fontSize">初始字号。</param>
        private static GameObject CreateText(string name, Transform parent, float fontSize)
        {
            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return textObject;
        }

        /// <summary>
        /// 设置 UI 对象的锚点范围并清空边距。
        /// </summary>
        /// <param name="rectTransform">需要设置的 RectTransform。</param>
        /// <param name="anchorMin">最小锚点。</param>
        /// <param name="anchorMax">最大锚点。</param>
        /// <param name="padding">四周统一内边距。</param>
        private static void SetStretch(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float padding = 0f)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>
        /// 返回父对象下指定名称的直接子对象。
        /// </summary>
        /// <param name="parent">需要检查的父级 Transform。</param>
        /// <param name="childName">需要匹配的直接子对象名称。</param>
        private static Transform GetDirectChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child;
            }
            return null;
        }

        /// <summary>
        /// 返回场景根级中指定名称的对象。
        /// </summary>
        /// <param name="scene">需要检查的场景。</param>
        /// <param name="objectName">需要匹配的根对象名称。</param>
        private static GameObject GetRootObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }
            return null;
        }
    }
}
