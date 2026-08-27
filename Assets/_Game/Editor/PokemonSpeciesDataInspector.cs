using Pokemon.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokemon.EditorTools
{
    [CustomEditor(typeof(PokemonSpeciesData))]
    public sealed class PokemonSpeciesDataInspector : UnityEditor.Editor
    {
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
            DrawProperty("BaseHP", "生命");
            DrawProperty("BaseAttack", "攻击");
            DrawProperty("BaseDefense", "防御");
            DrawProperty("BaseSpeed", "速度");
            DrawProperty("BaseSpAttack", "特攻");
            DrawProperty("BaseSpDefense", "特防");

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
