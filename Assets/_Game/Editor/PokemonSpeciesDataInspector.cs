using Pokemon.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokemon.EditorTools
{
    [CustomEditor(typeof(PokemonSpeciesData))]
    public sealed class PokemonSpeciesDataInspector : UnityEditor.Editor
    {
        private static readonly string[] BaseStatNames =
        {
            "BaseHP", "BaseAttack", "BaseDefense", "BaseSpeed", "BaseSpAttack", "BaseSpDefense"
        };

        private static readonly string[] BaseStatLabels =
        {
            "生命", "攻击", "防御", "速度", "特攻", "特防"
        };

        /// <summary>
        /// 使用中文分组和字段名称绘制精灵配置。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("身份信息");
            DrawProperty("ID", "精灵编号", "存档和配置引用使用的唯一编号。");
            DrawProperty("DisplayName", "显示名称");

            DrawSection("属性");
            DrawProperty("PrimaryType", "主属性");
            DrawProperty("primaryTypeIcon", "主属性图标", "从 Resources 下拖入对应的属性图片。");
            DrawProperty("SecondaryType", "副属性");
            DrawProperty("secondaryTypeIcon", "副属性图标", "没有副属性时保持为空。");

            DrawSection("种族值");
            DrawBaseStats();

            DrawSection("击败奖励");
            DrawProperty("BaseExpYield", "基础经验");
            DrawProperty("EvYieldHP", "生命学习力");
            DrawProperty("EvYieldAttack", "攻击学习力");
            DrawProperty("EvYieldDefense", "防御学习力");
            DrawProperty("EvYieldSpeed", "速度学习力");
            DrawProperty("EvYieldSpAttack", "特攻学习力");
            DrawProperty("EvYieldSpDefense", "特防学习力");

            DrawSection("战斗配置");
            DrawProperty("Abilities", "特性列表", null, true);
            DrawProperty("InitialSkills", "初始技能", null, true);
            DrawProperty("BattleSprite", "战斗图片");
            DrawProperty("CatchRate", "基础捕获率", "范围为 0 到 255，数值越高越容易捕获。");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 绘制种族值总点数与六项分配滑块，并限制已分配点数不超过总量。
        /// </summary>
        private void DrawBaseStats()
        {
            var stats = new SerializedProperty[BaseStatNames.Length];
            int assigned = 0;
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] = serializedObject.FindProperty(BaseStatNames[i]);
                assigned += stats[i].intValue;
            }

            SerializedProperty totalProperty = serializedObject.FindProperty("BaseStatTotal");
            int total = totalProperty.intValue == 0 ? assigned : totalProperty.intValue;
            int enteredTotal = Mathf.Max(0, EditorGUILayout.IntField(
                new GUIContent("种族值总点数", "旧精灵默认使用当前六项之和；修改总点数时按界面顺序保留已有分配。"),
                total));
            if (enteredTotal != total)
            {
                total = enteredTotal;
                totalProperty.intValue = total;
                int available = total;
                for (int i = 0; i < stats.Length; i++)
                {
                    stats[i].intValue = Mathf.Clamp(stats[i].intValue, 0, available);
                    available -= stats[i].intValue;
                }
                assigned = total - available;
            }

            for (int i = 0; i < stats.Length; i++)
            {
                int maximum = total - assigned + stats[i].intValue;
                int value = EditorGUILayout.IntSlider(BaseStatLabels[i], stats[i].intValue, 0, maximum);
                if (value == stats[i].intValue)
                    continue;

                assigned += value - stats[i].intValue;
                stats[i].intValue = value;
                totalProperty.intValue = total;
            }

            EditorGUILayout.LabelField($"已分配：{assigned} / {total}    剩余：{total - assigned}");
            if (assigned < total)
                EditorGUILayout.HelpBox("还有种族值未分配，六项之和达到总点数后即完成分配。", MessageType.Info);
        }

        /// <summary>
        /// 绘制一个中文配置分组标题。
        /// </summary>
        /// <param name="title">分组标题。</param>
        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        /// <summary>
        /// 使用中文名称和可选提示绘制一个序列化字段。
        /// </summary>
        /// <param name="propertyName">脚本中的序列化字段名称。</param>
        /// <param name="label">Inspector 显示的中文名称。</param>
        /// <param name="tooltip">鼠标悬停时显示的说明。</param>
        /// <param name="includeChildren">是否展开绘制数组或复合字段。</param>
        private void DrawProperty(
            string propertyName,
            string label,
            string tooltip = null,
            bool includeChildren = false)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(
                property,
                new GUIContent(label, tooltip),
                includeChildren);
        }
    }
}
