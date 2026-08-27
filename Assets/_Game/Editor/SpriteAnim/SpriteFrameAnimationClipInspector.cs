using System;
using System.Collections.Generic;
using Pokemon.Presentation.Animation;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Pokemon.EditorTools
{
    [CustomEditor(typeof(SpriteFrameAnimationClip))]
    public sealed class SpriteFrameAnimationClipInspector : UnityEditor.Editor
    {
        private SerializedProperty _frames;
        private SerializedProperty _framesPerSecond;
        private SerializedProperty _loop;
        private SerializedProperty _eventMarkers;
        private ReorderableList _frameList;
        private ReorderableList _eventList;
        private bool _showFrameList;
        private bool _showAdvancedEvents;
        private bool _appendOnImport;
        private bool _isPreviewPlaying;
        private int _previewFrame;
        private double _lastPreviewTime;

        /// <summary>
        /// 缓存序列化字段、创建可排序列表并启动编辑器预览更新。
        /// </summary>
        private void OnEnable()
        {
            _frames = serializedObject.FindProperty("frames");
            _framesPerSecond = serializedObject.FindProperty("framesPerSecond");
            _loop = serializedObject.FindProperty("loop");
            _eventMarkers = serializedObject.FindProperty("eventMarkers");

            _frameList = new ReorderableList(
                serializedObject,
                _frames,
                true,
                true,
                true,
                true);
            _frameList.drawHeaderCallback = DrawFrameListHeader;
            _frameList.drawElementCallback = DrawFrameListElement;

            _eventList = new ReorderableList(
                serializedObject,
                _eventMarkers,
                true,
                true,
                true,
                true);
            _eventList.drawHeaderCallback = DrawEventListHeader;
            _eventList.drawElementCallback = DrawEventListElement;

            EditorApplication.update += UpdatePreview;
        }

        /// <summary>
        /// 停止编辑器预览更新，避免 Inspector 关闭后继续刷新。
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
        }

        /// <summary>
        /// 绘制中文批量导入、播放设置、动画预览和打击帧配置界面。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("播放设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _framesPerSecond,
                new GUIContent("每秒帧数", "控制动画播放速度，例如 12 表示每秒播放 12 张图片。"));
            EditorGUILayout.PropertyField(
                _loop,
                new GUIContent("循环播放", "待机动画建议开启，技能动画建议关闭。"));

            EditorGUILayout.Space(8f);
            DrawBatchImportArea();

            EditorGUILayout.Space(6f);
            _showFrameList = EditorGUILayout.Foldout(
                _showFrameList,
                $"手动调整帧顺序（共 {_frames.arraySize} 帧）",
                true);
            if (_showFrameList)
                _frameList.DoLayoutList();

            EditorGUILayout.Space(8f);
            DrawPreview();

            EditorGUILayout.Space(8f);
            DrawImpactSettings();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 绘制批量拖入区域，并处理从 Project 窗口拖入的图片或文件夹。
        /// </summary>
        private void DrawBatchImportArea()
        {
            EditorGUILayout.LabelField("批量导入", EditorStyles.boldLabel);
            _appendOnImport = EditorGUILayout.ToggleLeft(
                "追加到现有帧（关闭时会替换现有帧）",
                _appendOnImport);

            Rect dropArea = GUILayoutUtility.GetRect(
                0f,
                72f,
                GUILayout.ExpandWidth(true));
            GUI.Box(
                dropArea,
                "把多张 Sprite、Sprite Sheet 或图片文件夹拖到这里\n会按照名称中的数字自动排序",
                EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导入当前选中的图片"))
                    ImportSprites(CollectSprites(Selection.objects));
                if (GUILayout.Button("重新按名称排序"))
                    SortCurrentFrames();
            }
        }

        /// <summary>
        /// 接收拖拽对象，并在用户松开鼠标时批量导入其中的 Sprite。
        /// </summary>
        /// <param name="dropArea">允许接收资源拖拽的 Inspector 区域。</param>
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            if (!dropArea.Contains(currentEvent.mousePosition)) return;
            if (currentEvent.type != EventType.DragUpdated &&
                currentEvent.type != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                ImportSprites(CollectSprites(DragAndDrop.objectReferences));
            }

            currentEvent.Use();
        }

        /// <summary>
        /// 从 Sprite、纹理或文件夹对象中收集全部可用帧，并进行自然数字排序。
        /// </summary>
        /// <param name="objects">用户选择或拖入的 Unity 资源对象。</param>
        /// <returns>去重并排序后的 Sprite 列表。</returns>
        private static List<Sprite> CollectSprites(UnityEngine.Object[] objects)
        {
            var sprites = new List<Sprite>();
            var spriteIds = new HashSet<int>();

            for (int i = 0; i < objects.Length; i++)
            {
                UnityEngine.Object current = objects[i];
                if (current is Sprite sprite)
                {
                    AddUniqueSprite(sprites, spriteIds, sprite);
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(current);
                if (string.IsNullOrEmpty(assetPath)) continue;

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    string[] textureGuids = AssetDatabase.FindAssets(
                        "t:Texture2D",
                        new[] { assetPath });
                    for (int guidIndex = 0; guidIndex < textureGuids.Length; guidIndex++)
                    {
                        AddSpritesAtPath(
                            AssetDatabase.GUIDToAssetPath(textureGuids[guidIndex]),
                            sprites,
                            spriteIds);
                    }
                }
                else
                {
                    AddSpritesAtPath(assetPath, sprites, spriteIds);
                }
            }

            sprites.Sort(CompareSpriteNamesNaturally);
            return sprites;
        }

        /// <summary>
        /// 加载指定资源路径下的所有 Sprite 子资源并加入结果列表。
        /// </summary>
        /// <param name="assetPath">纹理或 Sprite Sheet 的项目路径。</param>
        /// <param name="sprites">用于保存收集结果的列表。</param>
        /// <param name="spriteIds">用于过滤重复对象的实例编号集合。</param>
        private static void AddSpritesAtPath(
            string assetPath,
            List<Sprite> sprites,
            HashSet<int> spriteIds)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    AddUniqueSprite(sprites, spriteIds, sprite);
            }
        }

        /// <summary>
        /// 将尚未收录的 Sprite 加入结果，避免同一图片被重复导入。
        /// </summary>
        /// <param name="sprites">用于保存收集结果的列表。</param>
        /// <param name="spriteIds">已经收录的 Sprite 实例编号。</param>
        /// <param name="sprite">准备加入的 Sprite。</param>
        private static void AddUniqueSprite(
            List<Sprite> sprites,
            HashSet<int> spriteIds,
            Sprite sprite)
        {
            if (spriteIds.Add(sprite.GetInstanceID()))
                sprites.Add(sprite);
        }

        /// <summary>
        /// 比较两个 Sprite 名称，使 2.png 排在 10.png 之前。
        /// </summary>
        /// <param name="left">左侧 Sprite。</param>
        /// <param name="right">右侧 Sprite。</param>
        /// <returns>用于列表排序的比较结果。</returns>
        private static int CompareSpriteNamesNaturally(Sprite left, Sprite right)
        {
            string leftName = left.name;
            string rightName = right.name;
            int leftIndex = 0;
            int rightIndex = 0;

            while (leftIndex < leftName.Length && rightIndex < rightName.Length)
            {
                char leftCharacter = leftName[leftIndex];
                char rightCharacter = rightName[rightIndex];

                if (char.IsDigit(leftCharacter) && char.IsDigit(rightCharacter))
                {
                    long leftNumber = ReadNumber(leftName, ref leftIndex);
                    long rightNumber = ReadNumber(rightName, ref rightIndex);
                    int numberComparison = leftNumber.CompareTo(rightNumber);
                    if (numberComparison != 0) return numberComparison;
                    continue;
                }

                int characterComparison = char.ToUpperInvariant(leftCharacter)
                    .CompareTo(char.ToUpperInvariant(rightCharacter));
                if (characterComparison != 0) return characterComparison;
                leftIndex++;
                rightIndex++;
            }

            return leftName.Length.CompareTo(rightName.Length);
        }

        /// <summary>
        /// 从名称当前位置读取连续数字，并把位置推进到数字之后。
        /// </summary>
        /// <param name="value">包含数字的资源名称。</param>
        /// <param name="index">当前读取位置，返回时指向数字后的字符。</param>
        /// <returns>读取到的非负整数。</returns>
        private static long ReadNumber(string value, ref int index)
        {
            long number = 0;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                int digit = value[index] - '0';
                number = number <= (long.MaxValue - digit) / 10
                    ? number * 10 + digit
                    : long.MaxValue;
                index++;
            }

            return number;
        }

        /// <summary>
        /// 将批量收集到的 Sprite 替换或追加到当前动画帧列表。
        /// </summary>
        /// <param name="importedSprites">准备导入并已完成排序的 Sprite。</param>
        private void ImportSprites(List<Sprite> importedSprites)
        {
            if (importedSprites.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "没有找到图片帧",
                    "请选择 Sprite、已经导入为 Sprite 的图片或包含这些图片的文件夹。",
                    "确定");
                return;
            }

            if (!_appendOnImport && _frames.arraySize > 0 &&
                !EditorUtility.DisplayDialog(
                    "替换现有帧",
                    $"当前动画已有 {_frames.arraySize} 帧，是否替换为新导入的 {importedSprites.Count} 帧？",
                    "替换",
                    "取消"))
            {
                return;
            }

            var combinedSprites = new List<Sprite>();
            var spriteIds = new HashSet<int>();
            if (_appendOnImport)
            {
                for (int i = 0; i < _frames.arraySize; i++)
                {
                    Sprite existing = _frames.GetArrayElementAtIndex(i)
                        .objectReferenceValue as Sprite;
                    if (existing != null)
                        AddUniqueSprite(combinedSprites, spriteIds, existing);
                }
            }

            for (int i = 0; i < importedSprites.Count; i++)
                AddUniqueSprite(combinedSprites, spriteIds, importedSprites[i]);
            combinedSprites.Sort(CompareSpriteNamesNaturally);

            Undo.RecordObject(target, "批量导入序列帧");
            _frames.arraySize = combinedSprites.Count;
            for (int i = 0; i < combinedSprites.Count; i++)
                _frames.GetArrayElementAtIndex(i).objectReferenceValue = combinedSprites[i];

            _previewFrame = 0;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 按照资源名称中的文字和数字重新排列当前全部动画帧。
        /// </summary>
        private void SortCurrentFrames()
        {
            var sprites = new List<Sprite>();
            for (int i = 0; i < _frames.arraySize; i++)
            {
                Sprite sprite = _frames.GetArrayElementAtIndex(i)
                    .objectReferenceValue as Sprite;
                if (sprite != null) sprites.Add(sprite);
            }

            sprites.Sort(CompareSpriteNamesNaturally);
            Undo.RecordObject(target, "排序序列帧");
            _frames.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                _frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 绘制可播放、暂停和拖动帧位置的动画预览区域。
        /// </summary>
        private void DrawPreview()
        {
            EditorGUILayout.LabelField("动画预览", EditorStyles.boldLabel);
            if (_frames.arraySize == 0)
            {
                EditorGUILayout.HelpBox("请先批量导入动画图片。", MessageType.Info);
                return;
            }

            _previewFrame = Mathf.Clamp(_previewFrame, 0, _frames.arraySize - 1);
            Sprite currentSprite = GetSpriteAt(_previewFrame);
            Rect previewArea = GUILayoutUtility.GetRect(
                120f,
                260f,
                GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(previewArea, new Color(0.12f, 0.12f, 0.12f, 1f));
            if (currentSprite != null)
                DrawSpritePreview(previewArea, currentSprite);

            EditorGUILayout.LabelField(
                $"当前：第 {_previewFrame + 1} 帧 / 共 {_frames.arraySize} 帧",
                EditorStyles.centeredGreyMiniLabel);
            _previewFrame = EditorGUILayout.IntSlider(
                "预览帧",
                _previewFrame,
                0,
                _frames.arraySize - 1);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("上一帧"))
                    _previewFrame = Mathf.Max(0, _previewFrame - 1);
                if (GUILayout.Button(_isPreviewPlaying ? "暂停" : "播放"))
                {
                    _isPreviewPlaying = !_isPreviewPlaying;
                    _lastPreviewTime = EditorApplication.timeSinceStartup;
                }
                if (GUILayout.Button("下一帧"))
                    _previewFrame = Mathf.Min(_frames.arraySize - 1, _previewFrame + 1);
            }
        }

        /// <summary>
        /// 将 Sprite 在固定预览区域中按原始宽高比居中绘制。
        /// </summary>
        /// <param name="previewArea">预览使用的最大绘制区域。</param>
        /// <param name="sprite">当前需要显示的 Sprite。</param>
        private static void DrawSpritePreview(Rect previewArea, Sprite sprite)
        {
            Rect spriteRect = sprite.rect;
            float spriteAspect = spriteRect.width / Mathf.Max(1f, spriteRect.height);
            float areaAspect = previewArea.width / Mathf.Max(1f, previewArea.height);
            Rect drawArea = previewArea;

            if (areaAspect > spriteAspect)
            {
                drawArea.width = previewArea.height * spriteAspect;
                drawArea.x += (previewArea.width - drawArea.width) * 0.5f;
            }
            else
            {
                drawArea.height = previewArea.width / spriteAspect;
                drawArea.y += (previewArea.height - drawArea.height) * 0.5f;
            }

            Rect textureRect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            var textureCoordinates = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(drawArea, texture, textureCoordinates, true);
        }

        /// <summary>
        /// 返回指定下标当前绑定的 Sprite。
        /// </summary>
        /// <param name="frameIndex">需要读取的帧下标。</param>
        /// <returns>对应 Sprite；该位置为空时返回 null。</returns>
        private Sprite GetSpriteAt(int frameIndex)
        {
            return _frames.GetArrayElementAtIndex(frameIndex)
                .objectReferenceValue as Sprite;
        }

        /// <summary>
        /// 按动画帧率推进编辑器预览，并在非循环动画末尾自动暂停。
        /// </summary>
        private void UpdatePreview()
        {
            if (!_isPreviewPlaying || _frames == null || _frames.arraySize == 0) return;

            double currentTime = EditorApplication.timeSinceStartup;
            double frameDuration = 1d / Math.Max(0.01d, _framesPerSecond.floatValue);
            if (currentTime - _lastPreviewTime < frameDuration) return;

            int advancedFrames = Math.Max(
                1,
                (int)((currentTime - _lastPreviewTime) / frameDuration));
            int nextFrame = _previewFrame + advancedFrames;
            if (_loop.boolValue)
            {
                _previewFrame = nextFrame % _frames.arraySize;
            }
            else if (nextFrame >= _frames.arraySize)
            {
                _previewFrame = _frames.arraySize - 1;
                _isPreviewPlaying = false;
            }
            else
            {
                _previewFrame = nextFrame;
            }

            _lastPreviewTime = currentTime;
            Repaint();
        }

        /// <summary>
        /// 绘制当前打击帧信息、一键设置按钮和可扩展事件列表。
        /// </summary>
        private void DrawImpactSettings()
        {
            EditorGUILayout.LabelField("打击帧", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"当前配置：{GetImpactFrameDescription()}",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("将预览帧设为打击帧"))
                    SetImpactFrame(_previewFrame);
                if (GUILayout.Button("清除打击帧"))
                    RemoveImpactFrames();
            }

            _showAdvancedEvents = EditorGUILayout.Foldout(
                _showAdvancedEvents,
                "高级动画事件",
                true);
            if (_showAdvancedEvents)
                _eventList.DoLayoutList();
        }

        /// <summary>
        /// 返回当前所有 impact 事件对应的人类可读帧说明。
        /// </summary>
        /// <returns>例如“第 4 帧”；没有配置时返回“未设置”。</returns>
        private string GetImpactFrameDescription()
        {
            var frameNumbers = new List<string>();
            for (int i = 0; i < _eventMarkers.arraySize; i++)
            {
                SerializedProperty marker = _eventMarkers.GetArrayElementAtIndex(i);
                if (marker.FindPropertyRelative("eventId").stringValue !=
                    SpriteFrameAnimationEventIds.Impact)
                {
                    continue;
                }

                int frameIndex = marker.FindPropertyRelative("frameIndex").intValue;
                frameNumbers.Add($"第 {frameIndex + 1} 帧");
            }

            return frameNumbers.Count > 0
                ? string.Join("、", frameNumbers)
                : "未设置";
        }

        /// <summary>
        /// 将当前唯一的 impact 事件移动到指定帧；不存在时创建一个。
        /// </summary>
        /// <param name="frameIndex">需要作为打击帧的零基下标。</param>
        private void SetImpactFrame(int frameIndex)
        {
            Undo.RecordObject(target, "设置动画打击帧");
            for (int i = 0; i < _eventMarkers.arraySize; i++)
            {
                SerializedProperty marker = _eventMarkers.GetArrayElementAtIndex(i);
                if (marker.FindPropertyRelative("eventId").stringValue !=
                    SpriteFrameAnimationEventIds.Impact)
                {
                    continue;
                }

                marker.FindPropertyRelative("frameIndex").intValue = frameIndex;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                return;
            }

            int markerIndex = _eventMarkers.arraySize;
            _eventMarkers.InsertArrayElementAtIndex(markerIndex);
            SerializedProperty newMarker = _eventMarkers.GetArrayElementAtIndex(markerIndex);
            newMarker.FindPropertyRelative("frameIndex").intValue = frameIndex;
            newMarker.FindPropertyRelative("eventId").stringValue =
                SpriteFrameAnimationEventIds.Impact;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 从事件列表中移除全部 impact 事件，保留音效等其他扩展事件。
        /// </summary>
        private void RemoveImpactFrames()
        {
            Undo.RecordObject(target, "清除动画打击帧");
            for (int i = _eventMarkers.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty marker = _eventMarkers.GetArrayElementAtIndex(i);
                if (marker.FindPropertyRelative("eventId").stringValue ==
                    SpriteFrameAnimationEventIds.Impact)
                {
                    _eventMarkers.DeleteArrayElementAtIndex(i);
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 绘制手动帧列表的中文标题。
        /// </summary>
        /// <param name="rect">标题可用的绘制区域。</param>
        private static void DrawFrameListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "序列帧图片（可拖动调整顺序）");
        }

        /// <summary>
        /// 绘制手动帧列表中的单个 Sprite 引用。
        /// </summary>
        /// <param name="rect">当前元素可用的绘制区域。</param>
        /// <param name="index">当前帧下标。</param>
        /// <param name="isActive">当前元素是否处于激活状态。</param>
        /// <param name="isFocused">当前元素是否拥有焦点。</param>
        private void DrawFrameListElement(
            Rect rect,
            int index,
            bool isActive,
            bool isFocused)
        {
            SerializedProperty element = _frames.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(
                rect,
                element,
                new GUIContent($"第 {index + 1} 帧"));
        }

        /// <summary>
        /// 绘制高级事件列表的中文列标题。
        /// </summary>
        /// <param name="rect">标题可用的绘制区域。</param>
        private static void DrawEventListHeader(Rect rect)
        {
            float frameWidth = 80f;
            EditorGUI.LabelField(
                new Rect(rect.x, rect.y, frameWidth, rect.height),
                "帧下标");
            EditorGUI.LabelField(
                new Rect(rect.x + frameWidth + 6f, rect.y, rect.width - frameWidth - 6f, rect.height),
                "事件标识");
        }

        /// <summary>
        /// 绘制高级事件列表中的帧下标和事件标识。
        /// </summary>
        /// <param name="rect">当前元素可用的绘制区域。</param>
        /// <param name="index">当前事件下标。</param>
        /// <param name="isActive">当前元素是否处于激活状态。</param>
        /// <param name="isFocused">当前元素是否拥有焦点。</param>
        private void DrawEventListElement(
            Rect rect,
            int index,
            bool isActive,
            bool isFocused)
        {
            SerializedProperty marker = _eventMarkers.GetArrayElementAtIndex(index);
            SerializedProperty frameIndex = marker.FindPropertyRelative("frameIndex");
            SerializedProperty eventId = marker.FindPropertyRelative("eventId");
            float frameWidth = 80f;
            rect.y += 1f;
            rect.height = EditorGUIUtility.singleLineHeight;

            frameIndex.intValue = EditorGUI.IntField(
                new Rect(rect.x, rect.y, frameWidth, rect.height),
                frameIndex.intValue);
            eventId.stringValue = EditorGUI.TextField(
                new Rect(rect.x + frameWidth + 6f, rect.y, rect.width - frameWidth - 6f, rect.height),
                eventId.stringValue);
        }
    }
}
