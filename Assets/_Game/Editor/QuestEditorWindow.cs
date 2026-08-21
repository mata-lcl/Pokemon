using System;
using System.Collections.Generic;
using Pokemon.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokemon.EditorTools
{
    public class QuestEditorWindow : EditorWindow
    {
        private const string QuestFolder = "Assets/_Game/Data/Quests";

        private readonly List<QuestDefinition> _quests = new List<QuestDefinition>();
        private readonly List<string> _validationMessages = new List<string>();
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _searchText = string.Empty;
        private QuestDefinition _selectedQuest;
        private UnityEditor.Editor _cachedEditor;

        /// <summary>
        /// 打开宝可梦任务编辑器窗口。
        /// </summary>
        [MenuItem("Tools/Pokemon/任务编辑器")]
        public static void OpenWindow()
        {
            GetWindow<QuestEditorWindow>("任务编辑器");
        }

        /// <summary>
        /// 窗口启用时重新加载项目中的任务资源。
        /// </summary>
        private void OnEnable()
        {
            RefreshQuestList();
        }

        /// <summary>
        /// 绘制任务列表、属性面板和校验结果。
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawQuestList();
            DrawQuestDetails();
            EditorGUILayout.EndHorizontal();
            DrawValidationMessages();
        }

        /// <summary>
        /// 绘制新建、刷新、校验和搜索工具栏。
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("新建任务", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                CreateQuest();
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(55f)))
                RefreshQuestList();
            if (GUILayout.Button("校验全部", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                ValidateAllQuests();
            GUILayout.FlexibleSpace();
            _searchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制支持标题和编号搜索的任务资源列表。
        /// </summary>
        private void DrawQuestList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            EditorGUILayout.LabelField($"任务列表 ({_quests.Count})", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            for (int i = 0; i < _quests.Count; i++)
            {
                QuestDefinition quest = _quests[i];
                if (!MatchesSearch(quest))
                    continue;

                GUIStyle style = quest == _selectedQuest
                    ? EditorStyles.miniButtonMid
                    : EditorStyles.miniButton;
                if (GUILayout.Button($"{quest.Title}  [{quest.QuestId}]", style))
                    SelectQuest(quest);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制当前选中任务的默认 Inspector 属性。
        /// </summary>
        private void DrawQuestDetails()
        {
            EditorGUILayout.BeginVertical();
            if (_selectedQuest == null)
            {
                EditorGUILayout.HelpBox("请从左侧选择任务，或创建一个新任务。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_selectedQuest.name, EditorStyles.boldLabel);
            if (GUILayout.Button("定位资源", GUILayout.Width(80f)))
                EditorGUIUtility.PingObject(_selectedQuest);
            EditorGUILayout.EndHorizontal();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            UnityEditor.Editor.CreateCachedEditor(_selectedQuest, null, ref _cachedEditor);
            _cachedEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制最近一次任务校验发现的问题。
        /// </summary>
        private void DrawValidationMessages()
        {
            if (_validationMessages.Count == 0)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("校验结果", EditorStyles.boldLabel);
            for (int i = 0; i < _validationMessages.Count; i++)
                EditorGUILayout.HelpBox(_validationMessages[i], MessageType.Warning);
        }

        /// <summary>
        /// 在固定任务资源目录创建带唯一编号的新任务。
        /// </summary>
        private void CreateQuest()
        {
            EnsureQuestFolder();
            QuestDefinition quest = CreateInstance<QuestDefinition>();
            SerializedObject serializedQuest = new SerializedObject(quest);
            serializedQuest.FindProperty("questId").stringValue =
                $"quest_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            serializedQuest.FindProperty("title").stringValue = "新任务";
            serializedQuest.ApplyModifiedPropertiesWithoutUndo();

            string path = AssetDatabase.GenerateUniqueAssetPath($"{QuestFolder}/Quest_New.asset");
            AssetDatabase.CreateAsset(quest, path);
            AssetDatabase.SaveAssets();
            RefreshQuestList();
            SelectQuest(quest);
        }

        /// <summary>
        /// 创建任务资源目录及缺失的父目录。
        /// </summary>
        private void EnsureQuestFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder(QuestFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Quests");
        }

        /// <summary>
        /// 重新查找并按任务编号排列项目中的全部任务资源。
        /// </summary>
        private void RefreshQuestList()
        {
            _quests.Clear();
            string[] guids = AssetDatabase.FindAssets("t:QuestDefinition");
            for (int i = 0; i < guids.Length; i++)
            {
                QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (quest != null)
                    _quests.Add(quest);
            }
            _quests.Sort((left, right) => string.Compare(
                left.QuestId,
                right.QuestId,
                StringComparison.Ordinal));
            Repaint();
        }

        /// <summary>
        /// 选中任务并同步 Unity 项目窗口中的当前对象。
        /// </summary>
        /// <param name="quest">需要选中的任务资源。</param>
        private void SelectQuest(QuestDefinition quest)
        {
            _selectedQuest = quest;
            Selection.activeObject = quest;
            GUI.FocusControl(null);
        }

        /// <summary>
        /// 判断任务标题或编号是否包含当前搜索文本。
        /// </summary>
        /// <param name="quest">需要进行搜索匹配的任务。</param>
        private bool MatchesSearch(QuestDefinition quest)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;
            return quest.Title.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   quest.QuestId.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 校验全部任务的编号、目标、奖励和前置关系。
        /// </summary>
        private void ValidateAllQuests()
        {
            _validationMessages.Clear();
            Dictionary<string, QuestDefinition> ids = new Dictionary<string, QuestDefinition>();
            for (int i = 0; i < _quests.Count; i++)
            {
                QuestDefinition quest = _quests[i];
                ValidateQuestId(quest, ids);
                ValidateQuestContents(quest);
                if (HasPrerequisiteCycle(quest, quest, new HashSet<QuestDefinition>()))
                    _validationMessages.Add($"{quest.name}：前置任务关系存在循环依赖。");
            }

            if (_validationMessages.Count == 0)
                _validationMessages.Add("校验通过：未发现任务配置问题。");
        }

        /// <summary>
        /// 校验任务编号是否为空或与其他任务重复。
        /// </summary>
        /// <param name="quest">需要校验的任务。</param>
        /// <param name="ids">当前已经登记的任务编号。</param>
        private void ValidateQuestId(
            QuestDefinition quest,
            Dictionary<string, QuestDefinition> ids)
        {
            if (string.IsNullOrWhiteSpace(quest.QuestId))
            {
                _validationMessages.Add($"{quest.name}：任务编号不能为空。");
                return;
            }

            if (ids.TryGetValue(quest.QuestId, out QuestDefinition duplicate))
                _validationMessages.Add($"{quest.name}：任务编号与 {duplicate.name} 重复。");
            else
                ids.Add(quest.QuestId, quest);
        }

        /// <summary>
        /// 校验任务目标和奖励所需的资源或编号是否已经填写。
        /// </summary>
        /// <param name="quest">需要校验内容的任务。</param>
        private void ValidateQuestContents(QuestDefinition quest)
        {
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = quest.Objectives[i];
                bool missingTarget =
                    objective.Type == QuestObjectiveType.DefeatPokemon && objective.TargetPokemon == null ||
                    objective.Type == QuestObjectiveType.CollectItem && objective.TargetItem == null ||
                    objective.Type == QuestObjectiveType.TalkToNpc &&
                    string.IsNullOrWhiteSpace(objective.TargetNpcId);
                if (missingTarget)
                    _validationMessages.Add($"{quest.name}：第 {i + 1} 个目标缺少目标资源或 NPC 编号。");
            }

            for (int i = 0; i < quest.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = quest.Rewards[i];
                if (reward.Type == QuestRewardType.Item && reward.Item == null)
                    _validationMessages.Add($"{quest.name}：第 {i + 1} 个道具奖励没有绑定道具资源。");
            }
        }

        /// <summary>
        /// 递归判断指定任务的前置链是否会返回起始任务。
        /// </summary>
        /// <param name="current">当前检查的任务。</param>
        /// <param name="origin">循环检查的起始任务。</param>
        /// <param name="visited">当前检查已经访问的任务集合。</param>
        private bool HasPrerequisiteCycle(
            QuestDefinition current,
            QuestDefinition origin,
            HashSet<QuestDefinition> visited)
        {
            if (!visited.Add(current))
                return false;

            for (int i = 0; i < current.Prerequisites.Count; i++)
            {
                QuestDefinition prerequisite = current.Prerequisites[i];
                if (prerequisite == origin)
                    return true;
                if (prerequisite != null &&
                    HasPrerequisiteCycle(prerequisite, origin, visited))
                    return true;
            }
            return false;
        }
    }
}
