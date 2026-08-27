# 任务与 NPC 对话系统使用说明

本文档介绍项目中任务系统和 NPC 对话系统的配置、场景绑定、运行流程与常见问题。

当前版本已经提供中文简化 Inspector：

- 所有任务状态、目标类型、奖励类型、播放条件和结束动作均显示为中文；
- 任务目标只显示当前类型需要的目标字段；
- 金币奖励会自动隐藏无用的道具字段；
- “始终可用”和“任务可推进”会自动隐藏无效的任务状态字段；
- 每个对话分支底部会显示当前配置的中文用途提示。

## 1. 系统概览

任务系统负责：

- 注册任务配置；
- 管理任务状态和目标进度；
- 接取、推进和提交任务；
- 发放金币和道具奖励；
- 保存、读取任务进度和金币；
- 向任务追踪界面提供当前进度。

NPC 对话系统负责：

- 检测玩家是否进入 NPC 交互范围；
- 显示交互提示；
- 根据任务状态选择对话分支；
- 逐句、逐字播放对话；
- 显示说话人、左右立绘并播放语音；
- 触发 NPC Animator 动画；
- 在对话结束后接取任务、推进对话目标或提交任务。

典型流程如下：

```text
玩家与任务 NPC 对话
    ↓
播放 Available 分支
    ↓
对话结束后接取任务
    ↓
玩家完成击败、收集或对话目标
    ↓
任务进入 ReadyToSubmit
    ↓
玩家返回任务 NPC
    ↓
播放提交对话并领取奖励
```

## 2. 主要代码位置

### 2.1 任务系统

| 文件 | 用途 |
| --- | --- |
| `Assets/_Game/scripts/Quest/QuestDefinition.cs` | 任务、目标和奖励配置结构 |
| `Assets/_Game/scripts/Quest/QuestRuntimeData.cs` | 可存档的任务状态与目标进度 |
| `Assets/_Game/scripts/Quest/QuestService.cs` | 接取、推进、提交和奖励发放逻辑 |
| `Assets/_Game/scripts/Quest/QuestManager.cs` | 注册当前游戏使用的任务配置 |
| `Assets/_Game/scripts/Presentation/Quest/QuestTrackerView.cs` | 任务追踪 UI |
| `Assets/_Game/Editor/QuestEditorWindow.cs` | 任务创建、搜索和校验工具 |

### 2.2 NPC 对话系统

| 文件 | 用途 |
| --- | --- |
| `Assets/_Game/scripts/Dialogue/NpcDialogueDefinition.cs` | NPC 信息、任务分支和对话行配置 |
| `Assets/_Game/scripts/Presentation/Dialogue/NpcInteractable.cs` | 场景 NPC 的对话配置入口 |
| `Assets/_Game/scripts/Presentation/Dialogue/NpcInteractor.cs` | 玩家附近 NPC 检测和交互输入 |
| `Assets/_Game/scripts/Presentation/Dialogue/DialogueController.cs` | 对话 UI、逐字播放、语音和任务动作 |

## 3. 任务状态

| 状态 | 含义 |
| --- | --- |
| `Locked` | 前置任务尚未完成，不能接取 |
| `Available` | 前置条件已满足，可以接取 |
| `Active` | 已接取，正在完成目标 |
| `ReadyToSubmit` | 全部目标已完成，可以提交 |
| `Completed` | 已提交并领取奖励 |
| `Failed` | 预留的失败状态，当前版本尚未提供失败流程 |

没有前置任务的任务，在注册后会自动从 `Locked` 变为 `Available`。

当全部前置任务均为 `Completed` 时，后续任务会自动变为 `Available`。

## 4. 创建任务

### 4.1 打开任务编辑器

在 Unity 菜单中选择：

```text
Tools > Pokemon > 任务编辑器
```

也可以在 Project 窗口右键选择：

```text
Create > Pokemon > 任务配置
```

点击“新建任务”后，任务资源默认创建在：

```text
Assets/_Game/Data/Quests
```

### 4.2 基本字段

| 字段 | 说明 |
| --- | --- |
| `Quest Id` | 任务唯一编号，存档依赖该编号，不要随意修改 |
| `Title` | 任务标题 |
| `Description` | 任务说明 |
| `Icon` | 可选任务图标 |
| `Category` | `Main` 主线、`Side` 支线、`Daily` 日常 |
| `Prerequisites` | 必须先完成的任务资源 |
| `Objectives` | 任务目标列表 |
| `Rewards` | 提交任务时发放的奖励列表 |

配置完成后点击“校验全部”。编辑器会检查：

- 空任务 ID；
- 重复任务 ID；
- 目标缺少宝可梦、道具或 NPC ID；
- 道具奖励没有绑定道具；
- 前置任务存在循环依赖。

## 5. 任务目标

### 5.1 击败宝可梦

配置：

```text
Type：Defeat Pokemon
Target Pokemon：目标 PokemonSpeciesData
Required Amount：需要击败的数量
```

玩家在战斗中胜利时，`BattleCoordinator` 会自动上报被击败宝可梦，不需要额外配置事件。

只有任务处于 `Active` 时才会累计进度。

### 5.2 收集道具

配置：

```text
Type：Collect Item
Target Item：目标 ItemData
Required Amount：需要获得的数量
```

通过 `PlayerParty.AddItem` 获得道具时会自动推进目标。

当前逻辑统计的是接取任务之后新获得的道具，不会在接取任务时自动读取背包中已经持有的数量。

任务奖励发放的道具同样会经过 `PlayerParty.AddItem`。如果后续任务已经处于 `Active`，该奖励也可以推进后续任务的收集目标。

### 5.3 与 NPC 对话

配置：

```text
Type：Talk To Npc
Target Npc Id：目标 NPC 的稳定编号
Required Amount：通常设置为 1
```

`Target Npc Id` 必须和对应 `NpcDialogueDefinition` 的 `Npc Id` 完全一致，包括大小写。

目标 NPC 的对话分支需要设置：

```text
Completion Action：Report Npc Talked
```

对话完整播放结束后才会上报目标进度。

## 6. 任务奖励

### 6.1 金币奖励

```text
Type：Money
Amount：金币数量
```

任务提交成功后，金币会加入 `PlayerParty.Money`，并随游戏存档保存。

### 6.2 道具奖励

```text
Type：Item
Item：需要发放的 ItemData
Amount：发放数量
```

任务提交成功后，道具会加入玩家背包。

奖励只会在任务由 `ReadyToSubmit` 成功转为 `Completed` 时发放一次。

需要特别区分以下三个时机：

```text
AcceptQuest
→ 只接取任务，不发奖励

ReportNpcTalked / 击败目标 / 收集目标
→ 只推进目标；全部完成后进入 ReadyToSubmit，仍不发奖励

SubmitQuest
→ 任务进入 Completed，并在此时发放金币或道具奖励
```

因此，完成目标对话后通常还需要返回任务发布者，再播放一个
`ReadyToSubmit + SubmitQuest` 分支，奖励才会进入背包。

## 7. 在场景中注册任务

在 `World` 场景创建空对象：

```text
QuestSystem
```

添加 `QuestManager`，然后把所有可能使用的 `QuestDefinition` 资源拖入：

```text
Quest Definitions
```

注意事项：

- 没有放入 `Quest Definitions` 的任务不会被注册；
- 没有注册的任务无法接取、推进或提交；
- 新增任务资源后，需要同步增加数组长度并完成绑定；
- 场景中只保留一个负责当前游戏任务注册的 `QuestManager`。

## 8. 配置任务追踪 UI

在 Canvas 中准备：

```text
QuestTracker
├── TitleText
└── ObjectivesText
```

给合适的 UI 对象添加 `QuestTrackerView`，绑定：

| 字段 | 绑定对象 |
| --- | --- |
| `Quest Manager` | 场景中的 `QuestSystem` |
| `Content Root` | 整个任务追踪 UI 根对象 |
| `Title Text` | 显示任务标题的 TMP 文本 |
| `Objectives Text` | 显示目标与数量的 TMP 文本 |

当前追踪器显示任务数组中第一个处于 `Active` 或 `ReadyToSubmit` 的任务。

## 9. 创建 NPC 对话配置

在 Project 窗口中右键选择：

```text
Create > Pokemon > NPC 对话配置
```

建议将资源放在：

```text
Assets/_Game/Data/Dialogues
```

### 9.1 NPC 基本信息

| 字段 | 说明 |
| --- | --- |
| `Npc Id` | NPC 稳定编号，对话任务依赖该编号 |
| `Display Name` | 交互提示和默认说话人名称 |
| `Default Portrait` | 对话行未单独配置立绘时使用的默认立绘 |
| `Branches` | 按任务状态区分的对话分支列表 |

不要在已有存档使用后随意修改 `Npc Id`。

## 10. 对话分支

系统按照 `Branches` 的列表顺序检查配置，播放第一个满足条件的分支。

### 10.1 分支条件

`Quest State` 用于任务相关对话：

```text
Condition：Quest State
Quest：关联的 QuestDefinition
Required Quest State：需要匹配的任务状态
```

`Quest Can Progress` 用于不需要区分中间状态的简单任务：

```text
Condition：Quest Can Progress
Quest：关联的 QuestDefinition
```

它会匹配 `Available`、`Active` 和 `ReadyToSubmit`，适合搭配
`CompleteNpcQuest` 实现“一次对话完成任务并领取奖励”。

`Always` 用于普通对话或默认兜底对话：

```text
Condition：Always
```

如果配置 `Always`，必须将它放在列表最后。放在前面会导致后面的任务状态分支永远无法播放。

### 10.2 对话结束动作

| 动作 | 用途 |
| --- | --- |
| `None` | 只播放对话，不改变任务 |
| `AcceptQuest` | 对话结束后接取分支绑定的任务 |
| `ReportNpcTalked` | 对话结束后上报当前 NPC ID |
| `SubmitQuest` | 对话结束后提交分支绑定的任务 |
| `CompleteNpcQuest` | 一次对话内接取任务、完成当前 NPC 对话目标并提交奖励 |

使用 `AcceptQuest` 或 `SubmitQuest` 时，分支的 `Quest` 必须绑定正确的任务资源。

如果任务内容只是“与当前 NPC 对话并立即领取奖励”，可以直接使用
`CompleteNpcQuest`，不需要分别配置接取、推进和提交三次对话。该动作要求任务目标
是当前 NPC 的 `Talk To Npc` 目标，并且目标 `Npc Id` 与对话资源一致。

每个可播放分支至少需要配置一行 `Lines`，否则该分支不会开始播放。

## 11. 对话行与演绎配置

每个分支可以包含多行 `Lines`。

| 字段 | 说明 |
| --- | --- |
| `Speaker Name` | 当前行说话人；为空时使用 NPC 的 `Display Name` |
| `Portrait` | 当前行立绘；为空时使用 NPC 的默认立绘 |
| `Portrait Side` | `Left` 左侧或 `Right` 右侧 |
| `Text` | 当前行文本 |
| `Voice Clip` | 可选语音片段 |
| `Animation Trigger` | 可选 NPC Animator Trigger 名称 |

当前已经支持通过配置演绎：

- 切换说话人名称；
- 切换左右立绘；
- 播放每句语音；
- 触发 NPC 动画；
- 逐字显示文本。

`Animation Trigger` 当前作用于正在交互的 NPC。只有填写触发器时才需要给 NPC 的 `Npc Interactable > Npc Animator` 绑定 Animator。

后续可以继续在对话行配置中扩展镜头、表情、移动、音效、屏幕震动、选项和剧情变量。

## 12. 创建对话 UI

在 `World` 场景中，可以先选中 `WorldCanvas`，然后使用一键工具：

```text
Tools > Pokemon > 场景 > 创建对话系统层级
```

工具由 `Assets/_Game/Editor/DialogueSceneHierarchyBuilder.cs` 提供。它会检查已有
`DialogueLayer` 和 `DialogueSystem`，不会重复生成同名层级。

当前标准层级如下：

```text
WorldCanvas
└── DialogueLayer
    ├── DialoguePanel
    │   ├── LeftPortraitRoot
    │   │   └── PortraitImage
    │   ├── RightPortraitRoot
    │   │   └── PortraitImage
    │   └── TextArea
    │       ├── SpeakerNameText
    │       ├── DialogueText
    │       └── ContinueIndicator
    └── InteractionPrompt
        └── PromptText

World 场景根级
└── DialogueSystem
    ├── DialogueController
    └── AudioSource
```

给 `DialogueSystem` 添加 `DialogueController`。不要把控制器直接添加到会被隐藏的 `DialoguePanel` 上。

### 12.1 DialogueController 必须绑定

| 字段 | 绑定对象 |
| --- | --- |
| `Dialogue Root` | `WorldCanvas/DialogueLayer/DialoguePanel` |
| `Speaker Name Text` | `DialoguePanel/TextArea/SpeakerNameText` |
| `Dialogue Text` | `DialoguePanel/TextArea/DialogueText` |
| `Player Movement` | 玩家对象的 `PlayerMovement` |

### 12.2 DialogueController 可选绑定

| 字段 | 用途 |
| --- | --- |
| `Continue Indicator` | `DialoguePanel/TextArea/ContinueIndicator` |
| `Left Portrait Root` | `DialoguePanel/LeftPortraitRoot` |
| `Left Portrait Image` | `LeftPortraitRoot/PortraitImage` 的 Image |
| `Right Portrait Root` | `DialoguePanel/RightPortraitRoot` |
| `Right Portrait Image` | `RightPortraitRoot/PortraitImage` 的 Image |
| `Voice Audio Source` | `DialogueSystem` 自己的 AudioSource |

其他播放参数：

```text
Characters Per Second：每秒显示字符数，建议 35
Advance Key：主要继续键，默认 Space
Alternate Advance Key：备用继续键，默认 Return
```

也可以把 UI Button 的 `OnClick` 绑定到 `DialogueController.Advance`，用于鼠标或触摸操作。

## 13. 配置玩家交互组件

给玩家对象添加 `NpcInteractor`，绑定：

| 字段 | 绑定对象 |
| --- | --- |
| `Dialogue Controller` | 场景中的 `DialogueSystem` |
| `Interaction Prompt` | `WorldCanvas/DialogueLayer/InteractionPrompt` |
| `Interaction Prompt Text` | `InteractionPrompt/PromptText` |
| `Interaction Key` | NPC 交互键，默认 `E` |

玩家进入 NPC 交互范围后，系统会自动显示：

```text
按 E 与 NPC名称 对话
```

存在多个候选 NPC 时，会选择距离玩家最近的一个。

## 14. 配置场景 NPC

推荐结构：

```text
NPC_Professor
├── SpriteRenderer
├── NpcInteractable
├── BoxCollider2D
└── InteractionRange
    └── CircleCollider2D
```

配置步骤：

1. 在 NPC 根对象添加 `NpcInteractable`；
2. 把对应 `NpcDialogueDefinition` 拖入 `Dialogue Definition`；
3. 使用动画演绎时，把 NPC Animator 拖入 `Npc Animator`；
4. 身体 `BoxCollider2D` 不勾选 `Is Trigger`；
5. 子对象 `InteractionRange` 的 `CircleCollider2D` 勾选 `Is Trigger`；
6. 调整触发器半径，使玩家能在合理距离交互。

交互范围可以放在 NPC 子对象上，系统会从触发器向父对象取得 `NpcInteractable`。

### 14.1 世界形象与对话立绘的区别

`NpcDialogueDefinition > Default Portrait` 只用于对话框立绘，不会让 NPC 自动显示在世界场景中。

NPC 的世界形象必须由场景对象上的 `SpriteRenderer` 提供：

```text
NPC_Professor
├── SpriteRenderer：世界地图中显示的 NPC 图片
└── NpcInteractable
    └── Dialogue Definition：对话配置资源
```

如果 NPC 不显示，需要检查：

- NPC 根对象和 Sprite 子对象是否启用；
- `SpriteRenderer > Sprite` 是否已经绑定；
- Sprite 颜色 Alpha 是否为 1；
- `Sorting Layer` 是否设置为 `Player`，避免被 `Ground` 遮挡；
- `Order in Layer` 是否合适，建议先使用 1；
- Sprite 是否因为 `Pixels Per Unit` 过大而太小；
- NPC 子对象的 `Local Position` 是否归零；
- NPC 根对象是否位于相机可见范围内；
- NPC 不应错误使用 `Player` Tag，建议使用 `Untagged`。

当前城市素材中的部分角色图片是 16×16 像素并使用 `Pixels Per Unit = 100`，
世界尺寸只有约 0.16。可以把 NPC Sprite 子对象的 Scale 暂时设置为 5～6，
再根据玩家大小调整。

推荐的 SpriteRenderer 配置：

```text
Sorting Layer：Player
Order in Layer：1
Color Alpha：1
```

### 14.2 推荐的 NPC 坐标结构

建议通过 NPC 根对象摆放世界位置，Sprite 子对象只保留局部偏移：

```text
NPC_Professor                 Position：实际世界位置
└── Visual                    Local Position：0, 0, 0
    └── SpriteRenderer
```

不要使用很大的父坐标和相反的子坐标互相抵消，否则在 Scene 窗口定位、移动和调试时容易混乱。

## 15. 第一个对话任务完整示例

假设任务资源为：

```text
Quest_001_FirstTalk
```

任务目标：

```text
Type：Talk To Npc
Target Npc Id：npc_xiaoming
Required Amount：1
```

### 15.1 博士配置

创建 `NpcDialogue_Professor`：

```text
Npc Id：npc_professor
Display Name：博士
```

分支一：

```text
Condition：Quest State
Quest：Quest_001_FirstTalk
Required Quest State：Available
Completion Action：AcceptQuest
Text：最近村口出现了一些异常，你能帮我去问问小明吗？
```

分支二：

```text
Condition：Quest State
Quest：Quest_001_FirstTalk
Required Quest State：Active
Completion Action：None
Text：小明就在村口，找到他以后再回来告诉我。
```

分支三：

```text
Condition：Quest State
Quest：Quest_001_FirstTalk
Required Quest State：ReadyToSubmit
Completion Action：SubmitQuest
Text：你回来了。看来小明那边没有危险，辛苦你了。
```

分支四：

```text
Condition：Quest State
Quest：Quest_001_FirstTalk
Required Quest State：Completed
Completion Action：None
Text：多亏你的帮助，调查才能顺利完成。
```

### 15.2 小明配置

创建 `NpcDialogue_Xiaoming`：

```text
Npc Id：npc_xiaoming
Display Name：小明
```

分支一：

```text
Condition：Quest State
Quest：Quest_001_FirstTalk
Required Quest State：Active
Completion Action：ReportNpcTalked
Text：博士让你来的？这里刚才确实有奇怪的声音，不过现在已经没事了。
```

分支二：

```text
Condition：Quest State
Quest：Quest_001_FirstTalk
Required Quest State：ReadyToSubmit
Completion Action：None
Text：快回去告诉博士吧，免得他担心。
```

### 15.3 运行结果

```text
第一次和博士对话
→ 任务由 Available 变为 Active

和小明对话
→ 对话结束后上报 npc_xiaoming
→ 任务由 Active 变为 ReadyToSubmit

再次和博士对话
→ 对话结束后提交任务
→ 任务变为 Completed
→ 发放金币或道具奖励
```

## 16. 存档与读档

现有 `SaveGameService` 会保存：

- 玩家金币；
- 每个任务的 `Quest Id`；
- 任务状态；
- 每项目标的当前进度。

读取存档时会恢复这些数据。旧版存档没有任务或金币字段时，Unity JSON 会使用默认值。

新游戏会执行任务状态重置；任务在进入包含 `QuestManager` 的场景并完成注册后，根据前置任务重新计算是否可接取。

## 17. 配置顺序和注意事项

推荐按以下顺序配置：

1. 创建任务资源；
2. 配置目标、奖励和前置任务；
3. 将任务加入场景 `QuestManager`；
4. 创建 NPC 对话资源；
5. 按任务状态配置对话分支；
6. 创建并绑定对话 UI；
7. 给玩家添加 `NpcInteractor`；
8. 给 NPC 添加 `NpcInteractable` 和触发范围；
9. 进入 Play Mode 测试完整流程；
10. 保存后重新读档，确认任务状态和进度正确。

重要规则：

- `Quest Id` 必须唯一；
- 对话任务的 `Target Npc Id` 必须等于 NPC 配置的 `Npc Id`；
- `Always` 分支必须放在任务状态分支后面；
- 分支至少包含一行对话；
- `AcceptQuest` 和 `SubmitQuest` 分支必须绑定任务；
- 场景中的任务必须加入 `QuestManager`；
- 对话 UI、玩家和 NPC 引用必须通过 Inspector 拖拽绑定。

## 18. 常见问题

### 18.1 靠近 NPC 没有提示

检查：

- 玩家是否添加 `NpcInteractor`；
- `Interaction Prompt` 和文本是否绑定；
- NPC 是否添加 `NpcInteractable`；
- NPC 是否绑定对话资源；
- `InteractionRange` 是否勾选 `Is Trigger`；
- 当前是否存在满足任务状态的对话分支；
- 对话资源是否被 Unity 正确导入。

### 18.2 按 E 没有开始对话

检查：

- `NpcInteractor` 是否绑定 `DialogueController`；
- 匹配到的分支是否至少有一行 `Lines`；
- `DialogueController` 是否绑定 `Dialogue Root`、名称文本和正文文本；
- `DialogueController` 是否放在独立对象上，而不是会被关闭的面板上。

### 18.3 一直播放普通对话

检查 `Always` 分支是否放在列表最前面。系统使用第一个满足条件的分支，`Always` 应放在最后。

### 18.4 对话结束后任务没有接取

检查：

- 分支是否设置 `Completion Action = AcceptQuest`；
- 分支的 `Quest` 是否绑定正确任务；
- 任务是否处于 `Available`；
- 任务是否已加入 `QuestManager`。

### 18.5 与目标 NPC 对话后没有进度

检查：

- 任务是否处于 `Active`；
- 目标是否为 `Talk To Npc`；
- 分支动作是否为 `ReportNpcTalked`；
- `Target Npc Id` 和 `Npc Id` 是否完全一致；
- 是否完整播放到了对话最后一行。

### 18.6 对话结束后没有提交任务

检查：

- 任务是否已经处于 `ReadyToSubmit`；
- 当前分支是否匹配 `ReadyToSubmit`；
- 动作是否为 `SubmitQuest`；
- 分支是否绑定正确任务。

### 18.7 对话期间玩家仍然移动

检查 `DialogueController > Player Movement` 是否绑定玩家身上的 `PlayerMovement`。

### 18.8 立绘或语音不播放

检查：

- 左右立绘 Root 和 Image 是否绑定；
- 当前对话行或 NPC 默认立绘是否配置；
- `Voice Audio Source` 是否绑定；
- 当前行 `Voice Clip` 是否配置。

### 18.9 动画没有触发

检查：

- NPC 的 `Npc Animator` 是否绑定；
- 当前行 `Animation Trigger` 是否填写；
- Animator Controller 中是否存在同名 Trigger；
- Trigger 名称大小写是否一致。

### 18.10 对话结束后没有获得任务奖励

先确认当前对话执行的动作：

```text
AcceptQuest：只接取任务
ReportNpcTalked：只完成对话目标
SubmitQuest：提交任务并发放奖励
```

检查：

- 任务是否已经从 `Active` 进入 `ReadyToSubmit`；
- 任务发布者是否配置 `Required Quest State = ReadyToSubmit` 分支；
- 该分支是否设置 `Completion Action = SubmitQuest`；
- 分支的 `Quest` 是否绑定正确任务；
- 任务资源的 `Rewards` 是否绑定正确道具和数量；
- 提交对话是否完整播放到最后一句；
- 如果使用两个 NPC，目标 NPC 应执行 `ReportNpcTalked`，任务发布者才执行 `SubmitQuest`。

以“博士委托”为例，正确顺序是：

```text
和博士对话 → AcceptQuest
和小明对话 → ReportNpcTalked
返回博士对话 → SubmitQuest → 发放 5 个精灵球
```

如果只有一个 NPC 测试，需要配置 `Available`、`Active` 和 `ReadyToSubmit`
三个分支，并分别与同一个 NPC 对话三次。

## 19. 建议测试清单

- 新游戏后，无前置任务是否为 `Available`；
- 第一次与任务 NPC 对话后是否进入 `Active`；
- 击败、收集和对话目标是否只在 `Active` 时推进；
- 全部目标完成后是否进入 `ReadyToSubmit`；
- 提交后是否进入 `Completed`；
- 奖励是否只发放一次；
- 后续任务是否在前置任务完成后解锁；
- 保存并读档后任务状态、进度和金币是否保持；
- `Available`、`Active`、`ReadyToSubmit` 和 `Completed` 是否播放不同对话；
- 对话播放期间玩家是否停止移动；
- 多个 NPC 靠近时是否选择最近的 NPC。

## 20. 任务栏与获得道具提示

### 20.1 推荐层级

打开 `World` 场景并选中 `WorldCanvas`，然后执行：

```text
Tools > Pokemon > 场景 > 创建任务栏与道具提示层级
```

工具只创建层级、布局和组件，不会自动填写 Inspector 引用。生成后记得保存场景。

在 `WorldCanvas` 下建立以下层级：

```text
WorldCanvas
├── WorldMenuPanel
│   └── QuestButton
├── QuestPanel（默认关闭）
│   ├── TitleText
│   └── Scroll View
│       └── Viewport
│           └── Content
│               └── QuestText
└── ItemNotificationSystem（保持开启）
    └── ItemAcquiredPopup（默认关闭）
        ├── ItemIcon
        └── MessageText
```

建议将 `QuestPanel` 做成居中的大面板，将 `ItemAcquiredPopup` 放在屏幕中央。

### 20.2 任务栏绑定

1. 在 `QuestPanel` 上添加 `QuestJournalView`。
2. 将场景中的 `QuestSystem > QuestManager` 拖到 `Quest Manager`。
3. 将 `QuestText` 拖到 `Quest Text`。
4. 在 `WorldCanvas > WorldMenuController` 中绑定：
   - `Quest Panel`：拖入 `QuestPanel`；
   - `Quest Button`：拖入 `QuestButton`；
   - `Quest Key`：保持 `J`。
5. `QuestButton` 不需要再手动配置 `On Click`，脚本会自动绑定，避免一次点击开关两次。

完成后按 `J` 或点击任务按钮都能打开任务栏，再按一次 `J` 或按 `Esc` 关闭。
任务栏会显示进行中、可提交和已完成任务；尚未接取的任务不会显示。

### 20.3 获得道具提示绑定

1. 在始终开启的 `ItemNotificationSystem` 上添加 `ItemAcquiredNotificationView`。
2. 绑定以下字段：
   - `Popup Root`：拖入 `ItemAcquiredPopup`；
   - `Item Icon`：拖入 `ItemIcon`；
   - `Message Text`：拖入 `MessageText`；
   - `Display Duration`：建议填写 `2`。
3. 将 `ItemAcquiredPopup` 默认设为关闭，但不要关闭 `ItemNotificationSystem`。

之后所有通过 `PlayerParty.AddItem` 获得的道具都会自动显示：

```text
获得 精灵球 ×5
```

如果一次连续获得多种道具，提示会依次显示。读档恢复背包时不会弹出获得提示。
