using System;
using System.Collections.Generic;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class PokemonSkillReplacementPanel : MonoBehaviour
    {
        [SerializeField] private PokemonSkillSlotView[] equippedSlots;
        [SerializeField] private PokemonSkillSlotView candidatePrefab;
        [SerializeField] private Transform candidateContent;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button closeButton;

        private readonly List<PokemonSkillSlotView> _candidateSlots = new List<PokemonSkillSlotView>();
        private MonsterRuntime _pokemon;
        private SkillData _selectedSkill;
        private Action _onChanged;

        /// <summary>绑定关闭按钮。</summary>
        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
        }

        /// <summary>显示指定精灵的装备技能和可替换技能。</summary>
        /// <param name="pokemon">需要编辑技能的精灵。</param>
        /// <param name="onChanged">技能成功替换后的刷新操作。</param>
        public void Show(MonsterRuntime pokemon, Action onChanged)
        {
            _pokemon = pokemon;
            _onChanged = onChanged;
            _selectedSkill = null;
            titleText.text = $"{pokemon.Species.DisplayName} · 技能替换";
            hintText.text = "点击候选技能，再点击装备槽；也可以拖拽候选技能到装备槽。";
            gameObject.SetActive(true);
            RefreshSlots();
        }

        /// <summary>关闭技能替换界面。</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            _pokemon = null;
            _selectedSkill = null;
            _onChanged = null;
        }

        /// <summary>刷新四个装备槽和去重后的候选技能列表。</summary>
        private void RefreshSlots()
        {
            for (int i = 0; i < MonsterRuntime.MaxEquippedSkillCount; i++)
            {
                int index = i;
                SkillData skill = i < _pokemon.EquippedSkills.Count ? _pokemon.EquippedSkills[i] : null;
                string text = skill == null ? $"{i + 1}. 空槽" :
                    $"{i + 1}. {skill.DisplayName}\nPP {_pokemon.CurrentPP[skill]}/{skill.MaxPP}";
                equippedSlots[i].Bind(skill, text, this,
                    () => ReplaceSkill(index, _selectedSkill),
                    candidate => ReplaceSkill(index, candidate), false, false);
            }

            var candidates = new List<SkillData>();
            foreach (SkillData skill in _pokemon.Species.InitialSkills)
            {
                if (skill != null && !candidates.Contains(skill))
                    candidates.Add(skill);
            }
            while (_candidateSlots.Count < candidates.Count)
                _candidateSlots.Add(Instantiate(candidatePrefab, candidateContent));
            for (int i = 0; i < _candidateSlots.Count; i++)
            {
                _candidateSlots[i].gameObject.SetActive(i < candidates.Count);
                if (i >= candidates.Count)
                    continue;
                SkillData skill = candidates[i];
                bool equipped = _pokemon.IsSkillEquipped(skill);
                string text = $"{skill.DisplayName} · {skill.Type}\n威力 {skill.Power} · PP {skill.MaxPP}";
                if (equipped)
                    text += " · 已装备";
                _candidateSlots[i].Bind(skill, text, this,
                    () => SelectSkill(skill), null, !equipped, skill == _selectedSkill);
            }
        }

        /// <summary>选择一个尚未装备的候选技能。</summary>
        /// <param name="skill">点击的候选技能。</param>
        private void SelectSkill(SkillData skill)
        {
            if (_pokemon.IsSkillEquipped(skill))
            {
                hintText.text = "该技能已经装备，请选择其他技能。";
                return;
            }
            _selectedSkill = skill;
            hintText.text = $"已选择 {skill.DisplayName}，请点击要替换的装备槽。";
            RefreshSlots();
        }

        /// <summary>将候选技能装备到目标槽，并通知详情界面刷新。</summary>
        /// <param name="slotIndex">目标装备槽的索引。</param>
        /// <param name="skill">需要装备的候选技能。</param>
        private void ReplaceSkill(int slotIndex, SkillData skill)
        {
            if (skill == null)
            {
                hintText.text = "请先选择候选技能。";
                return;
            }
            if (_pokemon.IsSkillEquipped(skill))
            {
                hintText.text = "该技能已经装备，请选择其他技能。";
                return;
            }
            if (slotIndex > _pokemon.EquippedSkills.Count)
            {
                hintText.text = $"请先填充第 {_pokemon.EquippedSkills.Count + 1} 个空槽。";
                return;
            }
            bool changed = slotIndex < _pokemon.EquippedSkills.Count
                ? _pokemon.TryReplaceEquippedSkill(slotIndex, skill)
                : _pokemon.TryEquipSkill(skill);
            if (!changed)
                return;
            _selectedSkill = null;
            hintText.text = $"已装备 {skill.DisplayName}。";
            RefreshSlots();
            _onChanged?.Invoke();
        }
    }
}
