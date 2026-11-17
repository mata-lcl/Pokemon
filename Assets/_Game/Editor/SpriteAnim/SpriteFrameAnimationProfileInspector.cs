using System.Collections.Generic;
using Pokemon.Presentation;
using Pokemon.Presentation.Animation;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Pokemon.EditorTools
{
    [CustomEditor(typeof(SpriteFrameAnimationProfile))]
    public sealed class SpriteFrameAnimationProfileInspector : UnityEditor.Editor
    {
        private static readonly string[] AnimationTabs =
        {
            "待机",
            "物理攻击",
            "特殊攻击",
            "状态技能"
        };

        private SerializedProperty _idle;
        private SerializedProperty _physicalAttack;
        private SerializedProperty _specialAttack;
        private SerializedProperty _statusSkill;
        private SerializedProperty _spineSkeletonData;
        private SerializedProperty _spineIdleAnimation;
        private SerializedProperty _spinePhysicalAttackAnimation;
        private SerializedProperty _spineSpecialAttackAnimation;
        private SerializedProperty _spineStatusSkillAnimation;
        private SerializedProperty _spineScale;
        private UnityEditor.Editor _clipEditor;
        private UnityEditor.Editor _spinePreviewEditor;
        private AnimationReferenceAsset _spinePreviewReference;
        private SkeletonDataAsset _previewSkeletonAsset;
        private Spine.SkeletonData _previewSkeletonData;
        private string _previewAnimationName;
        private bool _showAssetReferences;
        private int _selectedAnimationTab;

        /// <summary>
        /// 缓存四类动画引用，供中文标签页和子资源创建功能使用。
        /// </summary>
        private void OnEnable()
        {
            _idle = serializedObject.FindProperty("idle");
            _physicalAttack = serializedObject.FindProperty("physicalAttack");
            _specialAttack = serializedObject.FindProperty("specialAttack");
            _statusSkill = serializedObject.FindProperty("statusSkill");
            _spineSkeletonData = serializedObject.FindProperty("spineSkeletonData");
            _spineIdleAnimation = serializedObject.FindProperty("spineIdleAnimation");
            _spinePhysicalAttackAnimation = serializedObject.FindProperty("spinePhysicalAttackAnimation");
            _spineSpecialAttackAnimation = serializedObject.FindProperty("spineSpecialAttackAnimation");
            _spineStatusSkillAnimation = serializedObject.FindProperty("spineStatusSkillAnimation");
            _spineScale = serializedObject.FindProperty("spineScale");
            EditorApplication.update += RepaintSpinePreview;
        }

        /// <summary>
        /// 销毁标签页内部使用的动画编辑器实例。
        /// </summary>
        private void OnDisable()
        {
            if (_clipEditor != null)
                DestroyImmediate(_clipEditor);
            EditorApplication.update -= RepaintSpinePreview;
            ClearSpinePreview();
        }

        /// <summary>
        /// 绘制单一 Profile 内的四类中文动画标签页和一键创建入口。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                _spineSkeletonData,
                new GUIContent("Spine 骨骼资源"));
            if (_spineSkeletonData.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "选择下方的战斗动作，再从已导入的 Spine 动画中挑选并预览。",
                    MessageType.Info);
                EditorGUILayout.PropertyField(_spineScale, new GUIContent("尺寸微调倍率"));
                EditorGUILayout.Space(8f);
                _selectedAnimationTab = GUILayout.Toolbar(
                    _selectedAnimationTab,
                    AnimationTabs,
                    GUILayout.Height(26f));
                DrawSpineAnimationEditor();
            }
            else
            {
                ClearSpinePreview();
                EditorGUILayout.HelpBox(
                    "每只精灵只需要维护这一个动画配置。可以一键创建四种内置动画，再在下方标签页批量导入图片和预览。",
                    MessageType.Info);

                if (GUILayout.Button("一键创建缺失的四种动画", GUILayout.Height(28f)))
                    CreateMissingAnimationSubAssets();

                _showAssetReferences = EditorGUILayout.Foldout(
                    _showAssetReferences,
                    "动画资源引用（高级）",
                    true);
                if (_showAssetReferences)
                    DrawAnimationReferences();

                EditorGUILayout.Space(8f);
                _selectedAnimationTab = GUILayout.Toolbar(
                    _selectedAnimationTab,
                    AnimationTabs,
                    GUILayout.Height(26f));

                SerializedProperty selectedProperty = GetSelectedAnimationProperty();
                SpriteFrameAnimationClip selectedClip =
                    selectedProperty.objectReferenceValue as SpriteFrameAnimationClip;
                if (selectedClip == null)
                {
                    EditorGUILayout.HelpBox(
                        $"“{AnimationTabs[_selectedAnimationTab]}”尚未创建。点击上方按钮即可自动创建。",
                        MessageType.Warning);
                }
                else
                {
                    DrawSelectedClipEditor(selectedClip);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 绘制当前战斗类型的 Spine 动画选择和内嵌预览。
        /// </summary>
        private void DrawSpineAnimationEditor()
        {
            var skeletonAsset = _spineSkeletonData.objectReferenceValue as SkeletonDataAsset;
            Spine.SkeletonData skeletonData = skeletonAsset.GetSkeletonData(true);
            if (skeletonData == null)
            {
                ClearSpinePreview();
                EditorGUILayout.HelpBox("骨骼资源尚未成功导入。", MessageType.Warning);
                return;
            }

            SerializedProperty animationProperty = GetSelectedSpineAnimationProperty();
            var animationNames = new List<string> { "" };
            var animationLabels = new List<string> { "未选择" };
            foreach (Spine.Animation animation in skeletonData.Animations)
            {
                animationNames.Add(animation.Name);
                animationLabels.Add(animation.Name);
            }

            int currentIndex = animationNames.IndexOf(animationProperty.stringValue);
            int selectedIndex = EditorGUILayout.Popup(
                "Spine 动画",
                Mathf.Max(0, currentIndex),
                animationLabels.ToArray());
            if (selectedIndex != Mathf.Max(0, currentIndex))
                animationProperty.stringValue = animationNames[selectedIndex];

            if (currentIndex < 0)
                EditorGUILayout.HelpBox("当前动作名不在骨骼资源中，请重新选择。", MessageType.Warning);

            DrawSpinePreview(skeletonAsset, skeletonData, animationProperty.stringValue);
        }

        /// <summary>
        /// 返回当前标签页保存 Spine 动作名称的字段。
        /// </summary>
        /// <returns>当前战斗类型对应的动作名称字段。</returns>
        private SerializedProperty GetSelectedSpineAnimationProperty()
        {
            switch (_selectedAnimationTab)
            {
                case 1:
                    return _spinePhysicalAttackAnimation;
                case 2:
                    return _spineSpecialAttackAnimation;
                case 3:
                    return _spineStatusSkillAnimation;
                default:
                    return _spineIdleAnimation;
            }
        }

        /// <summary>
        /// 使用 Spine 自带的动画预览器显示当前选择的动作。
        /// </summary>
        /// <param name="skeletonAsset">当前配置绑定的 Spine 骨骼资源。</param>
        /// <param name="skeletonData">已解析的骨骼数据。</param>
        /// <param name="animationName">当前标签页选择的动作名称。</param>
        private void DrawSpinePreview(
            SkeletonDataAsset skeletonAsset,
            Spine.SkeletonData skeletonData,
            string animationName)
        {
            if (string.IsNullOrEmpty(animationName) ||
                skeletonData.FindAnimation(animationName) == null)
            {
                ClearSpinePreview();
                EditorGUILayout.HelpBox("选定一个 Spine 动画后可在这里预览。", MessageType.Info);
                return;
            }

            if (_previewSkeletonAsset != skeletonAsset ||
                _previewSkeletonData != skeletonData ||
                _previewAnimationName != animationName)
            {
                ClearSpinePreview();
                _spinePreviewReference = CreateInstance<AnimationReferenceAsset>();
                _spinePreviewReference.hideFlags = HideFlags.HideAndDontSave;
                var previewObject = new SerializedObject(_spinePreviewReference);
                previewObject.FindProperty("skeletonDataAsset").objectReferenceValue = skeletonAsset;
                previewObject.FindProperty("animationName").stringValue = animationName;
                previewObject.ApplyModifiedPropertiesWithoutUndo();
                _spinePreviewEditor = UnityEditor.Editor.CreateEditor(_spinePreviewReference);
                _previewSkeletonAsset = skeletonAsset;
                _previewSkeletonData = skeletonData;
                _previewAnimationName = animationName;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("动作预览", EditorStyles.boldLabel);
            Rect previewArea = GUILayoutUtility.GetRect(
                120f,
                260f,
                GUILayout.ExpandWidth(true));
            _spinePreviewEditor.OnInteractivePreviewGUI(previewArea, EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(animationName, EditorStyles.miniLabel);
                _spinePreviewEditor.OnPreviewSettings();
            }
        }

        /// <summary>
        /// 释放当前标签页使用的 Spine 动画预览资源。
        /// </summary>
        private void ClearSpinePreview()
        {
            if (_spinePreviewEditor != null)
                DestroyImmediate(_spinePreviewEditor);
            if (_spinePreviewReference != null)
                DestroyImmediate(_spinePreviewReference);
            _spinePreviewEditor = null;
            _spinePreviewReference = null;
            _previewSkeletonAsset = null;
            _previewSkeletonData = null;
            _previewAnimationName = null;
        }

        /// <summary>
        /// 在 Spine 动作播放期间刷新配置文件的内嵌预览。
        /// </summary>
        private void RepaintSpinePreview()
        {
            if (_spinePreviewEditor != null)
                Repaint();
        }

        /// <summary>
        /// 使用中文名称绘制四类动画的底层资源引用。
        /// </summary>
        private void DrawAnimationReferences()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_idle, new GUIContent("待机动画"));
            EditorGUILayout.PropertyField(_physicalAttack, new GUIContent("物理攻击动画"));
            EditorGUILayout.PropertyField(_specialAttack, new GUIContent("特殊攻击动画"));
            EditorGUILayout.PropertyField(_statusSkill, new GUIContent("状态技能动画"));
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 返回当前中文标签页对应的动画引用字段。
        /// </summary>
        /// <returns>待机、物理攻击、特殊攻击或状态技能字段。</returns>
        private SerializedProperty GetSelectedAnimationProperty()
        {
            switch (_selectedAnimationTab)
            {
                case 1:
                    return _physicalAttack;
                case 2:
                    return _specialAttack;
                case 3:
                    return _statusSkill;
                default:
                    return _idle;
            }
        }

        /// <summary>
        /// 在 Profile Inspector 内嵌显示选中动画的批量导入和预览编辑器。
        /// </summary>
        /// <param name="clip">当前标签页对应的序列帧动画。</param>
        private void DrawSelectedClipEditor(SpriteFrameAnimationClip clip)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"正在编辑：{AnimationTabs[_selectedAnimationTab]}",
                    EditorStyles.boldLabel);
                if (GUILayout.Button("单独选中动画", GUILayout.Width(110f)))
                    Selection.activeObject = clip;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            UnityEditor.Editor.CreateCachedEditor(
                clip,
                typeof(SpriteFrameAnimationClipInspector),
                ref _clipEditor);
            _clipEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 为当前 Profile 创建尚未绑定的四类动画，并作为同一资产的子资源保存。
        /// </summary>
        private void CreateMissingAnimationSubAssets()
        {
            string profilePath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(profilePath))
            {
                EditorUtility.DisplayDialog(
                    "无法创建动画",
                    "请先把精灵动画配置保存为项目资产。",
                    "确定");
                return;
            }

            serializedObject.Update();
            bool createdAny = false;
            createdAny |= CreateClipIfMissing(_idle, "待机", true);
            createdAny |= CreateClipIfMissing(_physicalAttack, "物理攻击", false);
            createdAny |= CreateClipIfMissing(_specialAttack, "特殊攻击", false);
            createdAny |= CreateClipIfMissing(_statusSkill, "状态技能", false);
            serializedObject.ApplyModifiedProperties();

            if (createdAny)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(profilePath);
            }
        }

        /// <summary>
        /// 在指定字段为空时创建一个序列帧子资源，并设置默认循环方式。
        /// </summary>
        /// <param name="clipProperty">用于保存新动画引用的 Profile 字段。</param>
        /// <param name="displayName">新子资源使用的中文动画名称。</param>
        /// <param name="loop">新动画默认是否循环播放。</param>
        /// <returns>本次确实创建了新动画时返回 true。</returns>
        private bool CreateClipIfMissing(
            SerializedProperty clipProperty,
            string displayName,
            bool loop)
        {
            if (clipProperty.objectReferenceValue != null) return false;

            var clip = CreateInstance<SpriteFrameAnimationClip>();
            clip.name = $"{target.name}_{displayName}";
            var clipSerializedObject = new SerializedObject(clip);
            clipSerializedObject.FindProperty("loop").boolValue = loop;
            clipSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(clip, $"创建{displayName}动画");
            AssetDatabase.AddObjectToAsset(clip, target);
            clipProperty.objectReferenceValue = clip;
            return true;
        }
    }

    [CustomEditor(typeof(PokemonBattleAnimationCatalog))]
    public sealed class PokemonBattleAnimationCatalogInspector : UnityEditor.Editor
    {
        private SerializedProperty _entries;

        /// <summary>
        /// 缓存精灵种族与动画配置的映射列表。
        /// </summary>
        private void OnEnable()
        {
            _entries = serializedObject.FindProperty("entries");
        }

        /// <summary>
        /// 使用中文名称绘制精灵战斗动画目录。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "每个出场精灵会根据种族从这里查找自己的动画配置。玩家换人时也会重新查找。",
                MessageType.Info);
            EditorGUILayout.PropertyField(
                _entries,
                new GUIContent("精灵动画映射"),
                true);
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(PokemonBattleAnimationEntry))]
    public sealed class PokemonBattleAnimationEntryDrawer : PropertyDrawer
    {
        /// <summary>
        /// 返回一个精灵动画映射元素所需的两行高度。
        /// </summary>
        /// <param name="property">当前映射元素。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f +
                   EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary>
        /// 使用中文字段绘制精灵种族和动画配置引用。
        /// </summary>
        /// <param name="position">当前元素可用的绘制区域。</param>
        /// <param name="property">当前映射元素。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            var firstLine = new Rect(position.x, position.y, position.width, lineHeight);
            var secondLine = new Rect(
                position.x,
                position.y + lineHeight + spacing,
                position.width,
                lineHeight);

            EditorGUI.PropertyField(
                firstLine,
                property.FindPropertyRelative("species"),
                new GUIContent("精灵种族"));
            EditorGUI.PropertyField(
                secondLine,
                property.FindPropertyRelative("animationProfile"),
                new GUIContent("动画配置"));
            EditorGUI.EndProperty();
        }
    }

    [CustomEditor(typeof(SpriteFrameAnimationPlayer))]
    public sealed class SpriteFrameAnimationPlayerInspector : UnityEditor.Editor
    {
        private SerializedProperty _spriteRenderer;

        /// <summary>
        /// 缓存序列帧播放器需要绑定的 SpriteRenderer。
        /// </summary>
        private void OnEnable()
        {
            _spriteRenderer = serializedObject.FindProperty("spriteRenderer");
        }

        /// <summary>
        /// 使用中文提示绘制序列帧播放器组件引用。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(
                _spriteRenderer,
                new GUIContent("精灵渲染器", "拖入当前 PlayerView 或 EnemyView 上已有的 SpriteRenderer。"));
            if (_spriteRenderer.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "需要绑定当前对象上负责显示精灵的 SpriteRenderer。",
                    MessageType.Warning);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(BattleUnitView))]
    public sealed class BattleUnitViewInspector : UnityEditor.Editor
    {
        private SerializedProperty _spriteRenderer;
        private SerializedProperty _frameAnimationPlayer;

        /// <summary>
        /// 缓存战斗精灵视图需要绑定的两个组件引用。
        /// </summary>
        private void OnEnable()
        {
            _spriteRenderer = serializedObject.FindProperty("spriteRenderer");
            _frameAnimationPlayer = serializedObject.FindProperty("frameAnimationPlayer");
        }

        /// <summary>
        /// 使用中文名称绘制战斗精灵视图的动画组件引用。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(
                _spriteRenderer,
                new GUIContent("精灵渲染器", "拖入当前对象上已有的 SpriteRenderer。"));
            EditorGUILayout.PropertyField(
                _frameAnimationPlayer,
                new GUIContent("序列帧播放器", "拖入当前对象上的 SpriteFrameAnimationPlayer。"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
