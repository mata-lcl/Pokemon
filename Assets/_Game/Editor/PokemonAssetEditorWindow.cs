using System;
using System.Collections.Generic;
using Pokemon.Domain;
using Pokemon.Presentation.Animation;
using UnityEditor;
using UnityEngine;

namespace Pokemon.EditorTools
{
    public sealed class PokemonAssetEditorWindow : EditorWindow
    {
        private const string SpeciesFolder = "Assets/_Game/Data/Species";
        private const string AnimationFolder = "Assets/Anim/SpriteFrameAnimation";

        private static readonly string[] EditorTabs =
        {
            "精灵配置",
            "精灵动画"
        };

        private readonly List<PokemonSpeciesData> _species =
            new List<PokemonSpeciesData>();
        private readonly List<SpriteFrameAnimationProfile> _animationProfiles =
            new List<SpriteFrameAnimationProfile>();

        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _searchText = string.Empty;
        private int _selectedTab;
        private UnityEngine.Object _selectedAsset;
        private UnityEditor.Editor _cachedEditor;

        /// <summary>
        /// 从统一的中文菜单打开精灵与动画编辑器。
        /// </summary>
        [MenuItem("工具/宝可梦/配置编辑器/精灵与动画编辑器")]
        public static void OpenWindow()
        {
            GetWindow<PokemonAssetEditorWindow>("精灵与动画编辑器");
        }

        /// <summary>
        /// 窗口启用时重新读取现有精灵和动画配置资源。
        /// </summary>
        private void OnEnable()
        {
            RefreshAssets();
        }

        /// <summary>
        /// 窗口关闭时销毁内嵌使用的资源编辑器实例。
        /// </summary>
        private void OnDisable()
        {
            if (_cachedEditor != null)
                DestroyImmediate(_cachedEditor);
        }

        /// <summary>
        /// 绘制资源类型标签、工具栏、资源列表和中文配置面板。
        /// </summary>
        private void OnGUI()
        {
            int nextTab = GUILayout.Toolbar(_selectedTab, EditorTabs, GUILayout.Height(28f));
            if (nextTab != _selectedTab)
            {
                _selectedTab = nextTab;
                _selectedAsset = null;
                GUI.FocusControl(null);
            }

            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawAssetList();
            DrawAssetDetails();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制新建、刷新和搜索操作栏。
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string createLabel = _selectedTab == 0 ? "新建精灵" : "新建精灵动画";
            if (GUILayout.Button(createLabel, EditorStyles.toolbarButton, GUILayout.Width(100f)))
            {
                if (_selectedTab == 0)
                    CreateSpecies();
                else
                    CreateAnimationProfile();
            }

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(55f)))
                RefreshAssets();
            GUILayout.FlexibleSpace();
            _searchText = GUILayout.TextField(
                _searchText,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(220f));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制当前资源类型的可搜索列表。
        /// </summary>
        private void DrawAssetList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260f));
            int count = _selectedTab == 0 ? _species.Count : _animationProfiles.Count;
            EditorGUILayout.LabelField($"{EditorTabs[_selectedTab]}列表 ({count})", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            if (_selectedTab == 0)
            {
                for (int i = 0; i < _species.Count; i++)
                {
                    PokemonSpeciesData species = _species[i];
                    string label = $"{species.DisplayName}  [编号 {species.ID}]";
                    DrawAssetButton(species, label);
                }
            }
            else
            {
                for (int i = 0; i < _animationProfiles.Count; i++)
                {
                    SpriteFrameAnimationProfile profile = _animationProfiles[i];
                    DrawAssetButton(profile, profile.name);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制一个资源选择按钮，并按照当前搜索文字过滤。
        /// </summary>
        /// <param name="asset">按钮对应的项目资源。</param>
        /// <param name="label">按钮显示的中文名称。</param>
        private void DrawAssetButton(UnityEngine.Object asset, string label)
        {
            if (!MatchesSearch(label))
                return;

            GUIStyle style = asset == _selectedAsset
                ? EditorStyles.miniButtonMid
                : EditorStyles.miniButton;
            if (GUILayout.Button(label, style))
                SelectAsset(asset);
        }

        /// <summary>
        /// 绘制当前选中精灵或动画配置的内嵌 Inspector。
        /// </summary>
        private void DrawAssetDetails()
        {
            EditorGUILayout.BeginVertical();
            if (_selectedAsset == null)
            {
                EditorGUILayout.HelpBox(
                    $"请从左侧选择{EditorTabs[_selectedTab]}，或点击工具栏创建新资源。",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_selectedAsset.name, EditorStyles.boldLabel);
            if (GUILayout.Button("定位资源", GUILayout.Width(80f)))
                EditorGUIUtility.PingObject(_selectedAsset);
            EditorGUILayout.EndHorizontal();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            UnityEditor.Editor.CreateCachedEditor(_selectedAsset, null, ref _cachedEditor);
            _cachedEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 在固定精灵目录创建带有唯一编号的新精灵配置。
        /// </summary>
        private void CreateSpecies()
        {
            EnsureFolder(SpeciesFolder);
            var species = CreateInstance<PokemonSpeciesData>();
            species.ID = GetNextSpeciesId();
            species.DisplayName = "新精灵";
            species.name = $"精灵_{species.ID}";

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{SpeciesFolder}/{species.name}.asset");
            AssetDatabase.CreateAsset(species, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            SelectAsset(species);
        }

        /// <summary>
        /// 在固定动画目录创建精灵动画配置及四种默认动画子资源。
        /// </summary>
        private void CreateAnimationProfile()
        {
            EnsureFolder(AnimationFolder);
            var profile = CreateInstance<SpriteFrameAnimationProfile>();
            profile.name = "精灵动画配置_新建";
            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{AnimationFolder}/{profile.name}.asset");
            AssetDatabase.CreateAsset(profile, path);

            var serializedProfile = new SerializedObject(profile);
            CreateAnimationClip(profile, serializedProfile.FindProperty("idle"), "待机", true);
            CreateAnimationClip(
                profile,
                serializedProfile.FindProperty("physicalAttack"),
                "物理攻击",
                false);
            CreateAnimationClip(
                profile,
                serializedProfile.FindProperty("specialAttack"),
                "特殊攻击",
                false);
            CreateAnimationClip(
                profile,
                serializedProfile.FindProperty("statusSkill"),
                "状态技能",
                false);
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            RefreshAssets();
            SelectAsset(profile);
        }

        /// <summary>
        /// 创建指定类型的序列帧动画子资源并绑定到动画配置。
        /// </summary>
        /// <param name="profile">新动画所属的精灵动画配置。</param>
        /// <param name="clipProperty">保存新动画引用的配置字段。</param>
        /// <param name="displayName">动画使用的中文名称。</param>
        /// <param name="loop">动画是否默认循环播放。</param>
        private static void CreateAnimationClip(
            SpriteFrameAnimationProfile profile,
            SerializedProperty clipProperty,
            string displayName,
            bool loop)
        {
            var clip = CreateInstance<SpriteFrameAnimationClip>();
            clip.name = $"{profile.name}_{displayName}";
            var serializedClip = new SerializedObject(clip);
            serializedClip.FindProperty("loop").boolValue = loop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.AddObjectToAsset(clip, profile);
            clipProperty.objectReferenceValue = clip;
        }

        /// <summary>
        /// 重新查找并排序项目中的精灵和动画配置资源。
        /// </summary>
        private void RefreshAssets()
        {
            _species.Clear();
            string[] speciesGuids = AssetDatabase.FindAssets("t:PokemonSpeciesData");
            for (int i = 0; i < speciesGuids.Length; i++)
            {
                PokemonSpeciesData species = AssetDatabase.LoadAssetAtPath<PokemonSpeciesData>(
                    AssetDatabase.GUIDToAssetPath(speciesGuids[i]));
                if (species != null)
                    _species.Add(species);
            }
            _species.Sort((left, right) => left.ID.CompareTo(right.ID));

            _animationProfiles.Clear();
            string[] profileGuids = AssetDatabase.FindAssets("t:SpriteFrameAnimationProfile");
            for (int i = 0; i < profileGuids.Length; i++)
            {
                SpriteFrameAnimationProfile profile =
                    AssetDatabase.LoadAssetAtPath<SpriteFrameAnimationProfile>(
                        AssetDatabase.GUIDToAssetPath(profileGuids[i]));
                if (profile != null)
                    _animationProfiles.Add(profile);
            }
            _animationProfiles.Sort((left, right) => string.Compare(
                left.name,
                right.name,
                StringComparison.Ordinal));
            Repaint();
        }

        /// <summary>
        /// 返回当前全部精灵编号之后的下一个可用编号。
        /// </summary>
        /// <returns>大于现有最大编号的整数。</returns>
        private int GetNextSpeciesId()
        {
            int maximumId = -1;
            for (int i = 0; i < _species.Count; i++)
                maximumId = Mathf.Max(maximumId, _species[i].ID);
            return maximumId + 1;
        }

        /// <summary>
        /// 选中指定资源并同步 Unity 项目窗口中的活动对象。
        /// </summary>
        /// <param name="asset">需要选中的精灵或动画配置。</param>
        private void SelectAsset(UnityEngine.Object asset)
        {
            _selectedAsset = asset;
            Selection.activeObject = asset;
            GUI.FocusControl(null);
        }

        /// <summary>
        /// 判断资源显示名称是否符合当前搜索文字。
        /// </summary>
        /// <param name="label">资源在列表中的显示名称。</param>
        /// <returns>搜索为空或名称包含搜索文字时返回 true。</returns>
        private bool MatchesSearch(string label)
        {
            return string.IsNullOrWhiteSpace(_searchText) ||
                   label.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 逐级创建指定的项目资源目录。
        /// </summary>
        /// <param name="folderPath">以 Assets 开头的目标目录。</param>
        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                currentPath = nextPath;
            }
        }
    }
}
