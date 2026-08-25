using Pokemon.Presentation;
using Pokemon.Presentation.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pokemon.EditorTools
{
    public static class QuestItemUiHierarchyBuilder
    {
        private const string MenuPath = "Tools/Pokemon/场景/创建任务栏与道具提示层级";

        /// <summary>
        /// 在当前选中的 WorldCanvas 下创建任务按钮、任务面板和道具提示层级。
        /// </summary>
        [MenuItem(MenuPath)]
        private static void CreateHierarchy()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "World")
            {
                EditorUtility.DisplayDialog("创建任务栏与道具提示层级", "请先打开 World 场景。", "确定");
                return;
            }

            Canvas canvas = GetSelectedCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "创建任务栏与道具提示层级",
                    "请在 Hierarchy 中选中 WorldCanvas，或选中 WorldCanvas 下的任意对象。",
                    "确定");
                return;
            }

            Transform menuPanel = GetDirectChild(canvas.transform, "WorldMenuPanel");
            if (menuPanel == null)
            {
                EditorUtility.DisplayDialog(
                    "创建任务栏与道具提示层级",
                    "WorldCanvas 下没有找到 WorldMenuPanel。",
                    "确定");
                return;
            }

            bool hasQuestButton = GetDirectChild(menuPanel, "QuestButton") != null;
            bool hasQuestPanel = GetDirectChild(canvas.transform, "QuestPanel") != null;
            bool hasItemNotificationSystem =
                GetDirectChild(canvas.transform, "ItemNotificationSystem") != null;
            if (hasQuestButton && hasQuestPanel && hasItemNotificationSystem)
            {
                EditorUtility.DisplayDialog(
                    "创建任务栏与道具提示层级",
                    "任务栏与道具提示层级已经完整，不需要重复创建。",
                    "确定");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建任务栏与道具提示层级");

            if (!hasQuestButton)
                CreateQuestButton(menuPanel);
            if (!hasQuestPanel)
                CreateQuestPanel(canvas.transform);
            if (!hasItemNotificationSystem)
                CreateItemNotificationSystem(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = canvas.gameObject;

            EditorUtility.DisplayDialog(
                "创建任务栏与道具提示层级",
                "缺少的层级已补齐，原有对象保持不变。请保存 World 场景，并在 Inspector 中完成引用绑定。",
                "确定");
        }

        /// <summary>
        /// 仅在当前打开 World 场景时启用创建菜单。
        /// </summary>
        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateHierarchy()
        {
            return SceneManager.GetActiveScene().name == "World";
        }

        /// <summary>
        /// 在世界菜单中创建打开任务栏的按钮。
        /// </summary>
        /// <param name="menuPanel">任务按钮所属的世界菜单面板。</param>
        private static void CreateQuestButton(Transform menuPanel)
        {
            GameObject buttonObject = CreateUiObject("QuestButton", menuPanel);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, GetNextButtonY(menuPanel));
            rectTransform.sizeDelta = new Vector2(420f, 84f);

            Image background = buttonObject.AddComponent<Image>();
            background.color = new Color(0.84f, 0.91f, 0.95f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;

            GameObject labelObject = CreateText("Label", buttonObject.transform, 30f);
            SetStretch(labelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 8f);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = "任务（J）";
            label.color = new Color(0.08f, 0.13f, 0.17f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// 创建用于显示任务状态和目标进度的任务面板。
        /// </summary>
        /// <param name="canvasTransform">任务面板所属的 Canvas Transform。</param>
        private static void CreateQuestPanel(Transform canvasTransform)
        {
            GameObject questPanel = CreatePanel(
                "QuestPanel",
                canvasTransform,
                new Color(0.035f, 0.055f, 0.07f, 0.96f));
            SetStretch(
                questPanel.GetComponent<RectTransform>(),
                new Vector2(0.16f, 0.1f),
                new Vector2(0.84f, 0.9f));
            questPanel.AddComponent<QuestJournalView>();

            GameObject titleObject = CreateText("TitleText", questPanel.transform, 42f);
            SetStretch(
                titleObject.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.84f),
                new Vector2(0.94f, 0.96f));
            TMP_Text title = titleObject.GetComponent<TMP_Text>();
            title.text = "任务日志";
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;

            GameObject hintObject = CreateText("CloseHintText", questPanel.transform, 22f);
            SetStretch(
                hintObject.GetComponent<RectTransform>(),
                new Vector2(0.68f, 0.02f),
                new Vector2(0.94f, 0.08f));
            TMP_Text hint = hintObject.GetComponent<TMP_Text>();
            hint.text = "J / Esc 关闭";
            hint.color = new Color(0.68f, 0.76f, 0.8f, 1f);
            hint.alignment = TextAlignmentOptions.Right;

            CreateQuestScrollView(questPanel.transform);
            questPanel.SetActive(false);
        }

        /// <summary>
        /// 创建可滚动的任务文字区域及其内容层级。
        /// </summary>
        /// <param name="parent">滚动区域所属的任务面板 Transform。</param>
        private static void CreateQuestScrollView(Transform parent)
        {
            GameObject scrollView = CreatePanel(
                "ScrollView",
                parent,
                new Color(0.07f, 0.1f, 0.125f, 0.92f));
            SetStretch(
                scrollView.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.1f),
                new Vector2(0.94f, 0.82f));

            GameObject viewport = CreateUiObject("Viewport", scrollView.transform);
            SetStretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 18f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUiObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 22);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject questTextObject = CreateText("QuestText", content.transform, 27f);
            TMP_Text questText = questTextObject.GetComponent<TMP_Text>();
            questText.text = "当前没有已接取的任务。";
            questText.lineSpacing = 8f;

            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
        }

        /// <summary>
        /// 创建始终启用的道具提示控制对象和默认隐藏的中央提示面板。
        /// </summary>
        /// <param name="canvasTransform">道具提示系统所属的 Canvas Transform。</param>
        private static void CreateItemNotificationSystem(Transform canvasTransform)
        {
            GameObject notificationSystem = CreateUiObject("ItemNotificationSystem", canvasTransform);
            SetStretch(notificationSystem.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            notificationSystem.AddComponent<ItemAcquiredNotificationView>();
            notificationSystem.transform.SetAsLastSibling();

            GameObject popup = CreatePanel(
                "ItemAcquiredPopup",
                notificationSystem.transform,
                new Color(0.035f, 0.055f, 0.07f, 0.96f));
            RectTransform popupRect = popup.GetComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.anchoredPosition = Vector2.zero;
            popupRect.sizeDelta = new Vector2(640f, 116f);
            popup.GetComponent<Image>().raycastTarget = false;

            GameObject iconObject = CreateUiObject("ItemIcon", popup.transform);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(72f, 0f);
            iconRect.sizeDelta = new Vector2(76f, 76f);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            GameObject messageObject = CreateText("MessageText", popup.transform, 30f);
            SetStretch(
                messageObject.GetComponent<RectTransform>(),
                new Vector2(0.19f, 0.12f),
                new Vector2(0.95f, 0.88f));
            TMP_Text message = messageObject.GetComponent<TMP_Text>();
            message.text = "获得 精灵球 ×5";
            message.fontStyle = FontStyles.Bold;
            message.alignment = TextAlignmentOptions.MidlineLeft;

            popup.SetActive(false);
        }

        /// <summary>
        /// 返回世界菜单现有按钮下方的下一个纵向位置。
        /// </summary>
        /// <param name="menuPanel">需要检查按钮位置的世界菜单面板。</param>
        private static float GetNextButtonY(Transform menuPanel)
        {
            float minimumY = float.PositiveInfinity;
            for (int i = 0; i < menuPanel.childCount; i++)
            {
                RectTransform child = menuPanel.GetChild(i) as RectTransform;
                if (child != null && child.GetComponent<Button>() != null)
                    minimumY = Mathf.Min(minimumY, child.anchoredPosition.y);
            }

            return float.IsPositiveInfinity(minimumY) ? 0f : minimumY - 100f;
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
        /// <param name="parent">新对象所属的父级 Transform。</param>
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, $"创建 {name}");
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        /// <summary>
        /// 创建指定颜色的 UI 背景面板。
        /// </summary>
        /// <param name="name">面板对象名称。</param>
        /// <param name="parent">面板所属的父级 Transform。</param>
        /// <param name="color">面板背景颜色。</param>
        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreateUiObject(name, parent);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        /// <summary>
        /// 创建用于任务栏和道具提示的 TextMeshPro 文本对象。
        /// </summary>
        /// <param name="name">文本对象名称。</param>
        /// <param name="parent">文本所属的父级 Transform。</param>
        /// <param name="fontSize">文本初始字号。</param>
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
        /// 设置 UI 对象的锚点范围和统一内边距。
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
    }
}
