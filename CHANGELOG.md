# 更新日志 (Changelog)

本文档记录项目的所有重要变更，每次更新都会在此留下记录。

格式参考：[Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)

---

## [0.4.6] - 2026-08-12

### 新增功能
- 新增可配置的草丛遭遇池，支持按权重随机选择宝可梦，并随机生成遭遇等级。
- 新增基于移动距离的草丛遇敌检查，并在场景切换过程中保留遭遇宝可梦的种类和等级。
- 新增宝可梦队伍与仓库系统，队伍最多容纳六只宝可梦，支持队伍与仓库之间转移、搜索、排序和重排。
- 新增三个存档槽位的本地 JSON 存档系统，可保存玩家位置、队伍、仓库、背包、宝可梦成长数据、状态、特性、携带道具和技能 PP。

### UI / UX 改进
- 新增世界场景菜单，提供背包、宝可梦仓库和存档入口。
- 新增宝可梦仓库和详情界面，显示能力值、特性、携带道具和状态信息。
- 新增长按拖拽功能，支持调整宝可梦槽位和背包道具顺序。
- 更新战斗背包，使其能够保存并显示自定义道具顺序。

## [0.4.4] - 2026-08-05

### UI / UX Improvements
- Added the reusable `PokemonCollectionPanel` for party-based Pokemon switching.
- Added Pokemon slot details, active/fainted states, confirmation, and cancel actions.
- Replaced the legacy runtime-generated switch panel in battle UI flow.
- Updated the battle UI layout for the `1920x1080` CanvasScaler reference resolution.

## [0.4.2] - 2026-07-28

### 新增功能
- 新增 `TurnContext`、`SkillTurnAction` 回合数据模型，并完善 `TurnStep` 的回合结果记录。
- 新增回合管线阶段：行动排序、行动执行和回合末结算。
- 新增 `听桥` 应对技能，支持拦截伤害、反弹伤害，以及面对变化技能时的特殊处理。
- 新增 `IReactionSkillEffect` 与 `SkillReactionContext`，支持技能效果参与特殊行动时序。

### 重构与优化
- 重构 `ExecuteTurnUseCase`，将完整回合流程拆分为 `TurnPipeline`、`TurnPhase`、行动分发器和多个 Resolver。
- 新增 `ExecuteSingleAction` 和 `ExecuteEndOfTurn` 入口，支持道具回合复用敌方单次行动和回合末结算。
- 扩展 `EffectContext`，统一向技能效果传递双方宝可梦、伤害结果和反应技能上下文。
- 更新 `DamageEffect`，支持在造成伤害前交由应对技能拦截处理。

### Bug 修复
- 修复道具使用后的敌方反击仍调用完整回合入口的问题，避免重复创建玩家行动。
- 修复道具回合未正确追加中毒、灼伤等回合末结算步骤的问题。

## [0.2.0] - 2026-07-15

### ✨ 新功能

#### 道具与捕捉系统
- 实装**精灵球**使用逻辑，包含完整的捕捉概率算法（基于物种捕捉率、当前HP、状态异常等因子）
- 实装**伤药**使用逻辑，可恢复宝可梦HP，包含完整的UI交互流程
- 新增**背包与队伍管理模块**（`PlayerParty`），支持查看和管理持有的道具与宝可梦
- 新增**默认初始精灵球**，玩家初始获得若干基础精灵球

#### 宝可梦养成扩展
- 扩展 `MonsterRuntime`，新增**个体值（IV）**与**努力值（EV）**的生成与成长系统
- 扩展 `MonsterRuntime`，新增**等级成长事件**系统，升级时可触发相关事件
- 丰富宝可梦**物种数据**，新增捕捉率、对战奖励（经验值/金钱）等配置字段

### 🔧 重构与优化

#### 战斗系统大改版
- 对**战斗领域层（Domain）**与**表现层（Presentation）**进行了大规模重构，提升代码可维护性与扩展性
- 重构 `ExecuteTurnUseCase`，引入**回合步骤（TurnStep）快照**机制，便于回放与调试
- 增强 `ExecuteTurnUseCase`，新增**PP校验**逻辑，技能PP耗尽时触发相应处理
- 增强 `ExecuteTurnUseCase`，新增**特性危机触发**机制（如威吓、精神力等特性在危机时生效）
- 增强 `ExecuteTurnUseCase`，新增**回合结束（EOT）处理**，包含天气、状态持续回合数等结算
- 增强 `ExecuteTurnUseCase`，新增**回合结束动画**播放支持

#### 技能与效果管线重构
- 引入 `EffectContext`，作为技能效果执行的统一上下文对象
- 引入 `SkillEffectSO`（ScriptableObject），将技能效果配置化、数据驱动化
- 重构 `GetEffects` 方法，采用 **`effectConfigs + chance`（效果配置 + 触发概率）** 的驱动模式
- 技能效果现在支持配置化定义触发概率、持续回合、作用目标等参数

#### 伤害计算优化
- 优化 `DamageCalculator`，新增 **STAB（同属性加成）** 计算
- 优化 `DamageCalculator`，新增**属性克制倍率**计算（如水克火、火克草等）
- 优化 `DamageCalculator`，新增**特性钩子（Ability Hooks）**，允许特性在伤害计算阶段介入（如厚脂肪减半冰/火伤害）

### 🎨 UI / UX 改进

#### 战斗界面更新
- 更新战斗 UI，支持**道具列表**展示，玩家可在战斗中选择使用道具
- 更新战斗 UI，新增**道具按钮**，点击后展开道具使用面板
- 更新战斗 UI，新增**捕捉结果处理**流程（成功/失败动画与提示）
- 更新战斗 UI/控制器，优化整体交互体验与视觉反馈

#### 杂项修复
- 修复若干 UI/UX 问题，提升操作手感
- 统一代码注释与文案，部分英文注释翻译为中文并补充了说明
- 进行多项游戏性微调（数值平衡、动画节奏等）

---

## [0.1.0] - 2026-07-01

### ✨ 新功能
- 初始化项目，搭建基础框架
- 实现基础地图行走与场景切换
- 实现基础战斗框架（回合制战斗流程）
- 实现基础宝可梦数据模型与技能系统

---

<!--
未来更新模板：

## [版本号] - YYYY-MM-DD

### ✨ 新功能
- 描述新增的功能

### 🔧 重构与优化
- 描述重构和优化的内容

### 🎨 UI / UX 改进
- 描述界面和交互的改进

### 🐛 Bug 修复
- 描述修复的问题

### ⚡️ 性能优化
- 描述性能方面的改进

### 📝 其他
- 杂项更新
-->
