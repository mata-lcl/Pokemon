using Pokemon.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokemon.EditorTools
{
    [CustomEditor(typeof(QuestDefinition))]
    public class QuestDefinitionInspector : UnityEditor.Editor
    {
        private SerializedProperty _questId;
        private SerializedProperty _title;
        private SerializedProperty _description;
        private SerializedProperty _icon;
        private SerializedProperty _category;
        private SerializedProperty _prerequisites;
        private SerializedProperty _objectives;
        private SerializedProperty _rewards;

        /// <summary>
        /// 缓存任务配置中需要显示的序列化字段。
        /// </summary>
        private void OnEnable()
        {
            _questId = serializedObject.FindProperty("questId");
            _title = serializedObject.FindProperty("title");
            _description = serializedObject.FindProperty("description");
            _icon = serializedObject.FindProperty("icon");
            _category = serializedObject.FindProperty("category");
            _prerequisites = serializedObject.FindProperty("prerequisites");
            _objectives = serializedObject.FindProperty("objectives");
            _rewards = serializedObject.FindProperty("rewards");
        }

        /// <summary>
        /// 使用中文字段名绘制任务配置 Inspector。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _questId,
                new GUIContent("任务编号", "存档使用的唯一编号，创建存档后不要随意修改。"));
            EditorGUILayout.PropertyField(_title, new GUIContent("任务标题"));
            EditorGUILayout.PropertyField(_description, new GUIContent("任务描述"));
            EditorGUILayout.PropertyField(_icon, new GUIContent("任务图标"));
            EditorGUILayout.PropertyField(_category, new GUIContent("任务类型"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("任务关系", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _prerequisites,
                new GUIContent("前置任务", "列表中的任务全部完成后，当前任务才会变为可接取。"),
                true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("目标与奖励", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_objectives, new GUIContent("任务目标"), true);
            EditorGUILayout.PropertyField(_rewards, new GUIContent("任务奖励"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(NpcDialogueDefinition))]
    public class NpcDialogueDefinitionInspector : UnityEditor.Editor
    {
        private SerializedProperty _npcId;
        private SerializedProperty _displayName;
        private SerializedProperty _defaultPortrait;
        private SerializedProperty _branches;

        /// <summary>
        /// 缓存 NPC 对话配置中需要显示的序列化字段。
        /// </summary>
        private void OnEnable()
        {
            _npcId = serializedObject.FindProperty("npcId");
            _displayName = serializedObject.FindProperty("displayName");
            _defaultPortrait = serializedObject.FindProperty("defaultPortrait");
            _branches = serializedObject.FindProperty("branches");
        }

        /// <summary>
        /// 使用中文字段名和配置提示绘制 NPC 对话 Inspector。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("NPC 信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _npcId,
                new GUIContent("NPC 编号", "对话任务目标使用的唯一编号，例如 npc_xiaoming。"));
            EditorGUILayout.PropertyField(_displayName, new GUIContent("显示名称"));
            EditorGUILayout.PropertyField(
                _defaultPortrait,
                new GUIContent("默认对话立绘", "只用于对话框，不是世界场景中的 NPC 图片。"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "系统从上到下播放第一个满足条件的分支；“始终可用”分支请放在最后。",
                MessageType.Info);
            EditorGUILayout.PropertyField(_branches, new GUIContent("对话分支"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(QuestObjectiveDefinition))]
    public class QuestObjectiveDefinitionDrawer : PropertyDrawer
    {
        /// <summary>
        /// 根据目标折叠状态返回任务目标配置所需高度。
        /// </summary>
        /// <param name="property">当前任务目标序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return line;
            return line * 4f + EditorGUIUtility.standardVerticalSpacing * 3f;
        }

        /// <summary>
        /// 仅显示当前任务目标类型需要配置的目标字段。
        /// </summary>
        /// <param name="position">当前属性可用的绘制区域。</param>
        /// <param name="property">当前任务目标序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect current = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(
                current,
                property.isExpanded,
                "任务目标",
                true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                SerializedProperty type = property.FindPropertyRelative("type");
                current.y += line + spacing;
                EditorGUI.PropertyField(current, type, new GUIContent("目标类型"));

                current.y += line + spacing;
                switch ((QuestObjectiveType)type.enumValueIndex)
                {
                    case QuestObjectiveType.DefeatPokemon:
                        EditorGUI.PropertyField(
                            current,
                            property.FindPropertyRelative("targetPokemon"),
                            new GUIContent("目标宝可梦"));
                        break;
                    case QuestObjectiveType.CollectItem:
                        EditorGUI.PropertyField(
                            current,
                            property.FindPropertyRelative("targetItem"),
                            new GUIContent("目标道具"));
                        break;
                    case QuestObjectiveType.TalkToNpc:
                        EditorGUI.PropertyField(
                            current,
                            property.FindPropertyRelative("targetNpcId"),
                            new GUIContent("目标 NPC 编号"));
                        break;
                }

                current.y += line + spacing;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("requiredAmount"),
                    new GUIContent("需要数量"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(QuestRewardDefinition))]
    public class QuestRewardDefinitionDrawer : PropertyDrawer
    {
        /// <summary>
        /// 根据奖励折叠状态和奖励类型返回配置所需高度。
        /// </summary>
        /// <param name="property">当前任务奖励序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return line;

            SerializedProperty type = property.FindPropertyRelative("type");
            int fieldCount = (QuestRewardType)type.enumValueIndex == QuestRewardType.Item ? 3 : 2;
            return line * (fieldCount + 1) +
                   EditorGUIUtility.standardVerticalSpacing * fieldCount;
        }

        /// <summary>
        /// 金币奖励隐藏道具字段，道具奖励显示需要绑定的道具资源。
        /// </summary>
        /// <param name="position">当前属性可用的绘制区域。</param>
        /// <param name="property">当前任务奖励序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect current = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(
                current,
                property.isExpanded,
                "任务奖励",
                true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                SerializedProperty type = property.FindPropertyRelative("type");
                current.y += line + spacing;
                EditorGUI.PropertyField(current, type, new GUIContent("奖励类型"));

                if ((QuestRewardType)type.enumValueIndex == QuestRewardType.Item)
                {
                    current.y += line + spacing;
                    EditorGUI.PropertyField(
                        current,
                        property.FindPropertyRelative("item"),
                        new GUIContent("奖励道具"));
                }

                current.y += line + spacing;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("amount"),
                    new GUIContent("奖励数量"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(DialogueLineDefinition))]
    public class DialogueLineDefinitionDrawer : PropertyDrawer
    {
        /// <summary>
        /// 根据对话内容折叠状态和多行文本高度返回配置所需高度。
        /// </summary>
        /// <param name="property">当前对话内容序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return line;

            float textHeight = EditorGUI.GetPropertyHeight(
                property.FindPropertyRelative("text"),
                true);
            return line + EditorGUIUtility.standardVerticalSpacing +
                   line * 5f + textHeight +
                   EditorGUIUtility.standardVerticalSpacing * 5f;
        }

        /// <summary>
        /// 使用中文字段名绘制单句对话的演绎配置。
        /// </summary>
        /// <param name="position">当前属性可用的绘制区域。</param>
        /// <param name="property">当前对话内容序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect current = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(
                current,
                property.isExpanded,
                "对话内容",
                true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                current.y += line + spacing;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("speakerName"),
                    new GUIContent("说话人名称"));

                current.y += line + spacing;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("portrait"),
                    new GUIContent("当前立绘"));

                current.y += line + spacing;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("portraitSide"),
                    new GUIContent("立绘位置"));

                SerializedProperty text = property.FindPropertyRelative("text");
                float textHeight = EditorGUI.GetPropertyHeight(text, true);
                current.y += line + spacing;
                current.height = textHeight;
                EditorGUI.PropertyField(current, text, new GUIContent("对话文本"), true);

                current.y += textHeight + spacing;
                current.height = line;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("voiceClip"),
                    new GUIContent("语音（可选）"));

                current.y += line + spacing;
                EditorGUI.PropertyField(
                    current,
                    property.FindPropertyRelative("animationTrigger"),
                    new GUIContent("动画触发器（可选）"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(DialogueBranchDefinition))]
    public class DialogueBranchDefinitionDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 42f;

        /// <summary>
        /// 根据对话条件隐藏无效字段，并返回当前分支所需高度。
        /// </summary>
        /// <param name="property">当前对话分支序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded)
                return line;

            SerializedProperty condition = property.FindPropertyRelative("condition");
            DialogueBranchCondition conditionValue =
                (DialogueBranchCondition)condition.enumValueIndex;
            float height = line;
            height += spacing + line;
            if (conditionValue != DialogueBranchCondition.Always)
                height += spacing + line;
            if (conditionValue == DialogueBranchCondition.QuestState)
                height += spacing + line;
            height += spacing + EditorGUI.GetPropertyHeight(
                property.FindPropertyRelative("lines"),
                true);
            height += spacing + line;
            height += spacing + HelpBoxHeight;
            return height;
        }

        /// <summary>
        /// 绘制会随条件变化的中文对话分支配置。
        /// </summary>
        /// <param name="position">当前属性可用的绘制区域。</param>
        /// <param name="property">当前对话分支序列化数据。</param>
        /// <param name="label">Unity 提供的列表元素标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect current = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(
                current,
                property.isExpanded,
                "对话分支",
                true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                SerializedProperty condition = property.FindPropertyRelative("condition");
                current.y += line + spacing;
                EditorGUI.PropertyField(current, condition, new GUIContent("播放条件"));
                DialogueBranchCondition conditionValue =
                    (DialogueBranchCondition)condition.enumValueIndex;

                if (conditionValue != DialogueBranchCondition.Always)
                {
                    current.y += line + spacing;
                    EditorGUI.PropertyField(
                        current,
                        property.FindPropertyRelative("quest"),
                        new GUIContent("关联任务"));
                }

                if (conditionValue == DialogueBranchCondition.QuestState)
                {
                    current.y += line + spacing;
                    EditorGUI.PropertyField(
                        current,
                        property.FindPropertyRelative("requiredQuestState"),
                        new GUIContent("要求的任务状态"));
                }

                SerializedProperty lines = property.FindPropertyRelative("lines");
                float linesHeight = EditorGUI.GetPropertyHeight(lines, true);
                current.y += line + spacing;
                current.height = linesHeight;
                EditorGUI.PropertyField(current, lines, new GUIContent("对话内容"), true);

                current.y += linesHeight + spacing;
                current.height = line;
                SerializedProperty action = property.FindPropertyRelative("completionAction");
                EditorGUI.PropertyField(current, action, new GUIContent("对话结束后"));

                current.y += line + spacing;
                current.height = HelpBoxHeight;
                EditorGUI.HelpBox(
                    current,
                    GetConfigurationHint(conditionValue, (DialogueCompletionAction)action.enumValueIndex),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 返回当前条件和结束动作对应的中文配置提示。
        /// </summary>
        /// <param name="condition">当前对话分支条件。</param>
        /// <param name="action">当前对话结束动作。</param>
        private static string GetConfigurationHint(
            DialogueBranchCondition condition,
            DialogueCompletionAction action)
        {
            if (condition == DialogueBranchCondition.QuestCanProgress &&
                action == DialogueCompletionAction.CompleteNpcQuest)
                return "简化模式：第一次对话直接完成当前 NPC 任务并发放奖励。";

            switch (action)
            {
                case DialogueCompletionAction.AcceptQuest:
                    return "用于“可接取”状态：对话结束后接取任务。";
                case DialogueCompletionAction.ReportNpcTalked:
                    return "用于“进行中”状态：完成当前 NPC 的对话目标。";
                case DialogueCompletionAction.SubmitQuest:
                    return "用于“可提交”状态：对话结束后提交任务并发放奖励。";
                case DialogueCompletionAction.CompleteNpcQuest:
                    return "一次完成当前 NPC 对话任务，建议搭配“任务可推进（简化模式）”。";
                default:
                    return condition == DialogueBranchCondition.Always
                        ? "普通或兜底对话，不改变任务状态；请放在分支列表最后。"
                        : "只播放对话，不改变任务状态。";
            }
        }
    }
}
