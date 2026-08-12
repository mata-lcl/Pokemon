# Unity 项目代码修改规范

本项目是 Unity 项目。对本项目代码进行修改时，请遵守以下规则。

## Unity 对象引用规则

1. 当代码需要引用场景中的对象、组件、UI、Prefab 或其他 Unity 对象时，优先使用 Inspector 变量拖拽绑定。
2. 能通过 `[SerializeField] private` 字段暴露给 Inspector 进行拖拽绑定的对象，不要默认使用 `GameObject.Find`、`GameObject.FindWithTag`、`FindObjectOfType`、`FindFirstObjectByType`、`transform.Find` 等运行时查找方式。
3. 只有在以下情况下，才可以使用 Find 类查找方式：

   * 目标对象是运行时动态生成的；
   * 用户明确要求使用 Find 类方法；
   * 当前场景结构无法通过 Inspector 拖拽稳定绑定，并且已经说明原因。

## 代码修改规则

1. 修改代码时要保持简洁、清晰，并具有可拓展性。
2. 只修改与当前问题直接相关的代码，不要顺手重构、改名、调整格式或改动无关逻辑。
3. 不要修改原代码中的注释，除非用户明确要求修改注释。
4. 不要删除已有功能、已有字段或已有方法，除非用户明确要求删除，或者删除是解决当前问题所必需的，并且需要说明原因。
5. 如果需要新增字段用于 Inspector 拖拽绑定，优先使用如下形式：

```csharp
[SerializeField] private GameObject targetObject;
```

6. 修改 Unity 项目代码时，如果用户没有明确要求，不要额外编写兜底逻辑、兼容逻辑或防御性分支；只处理当前问题直接需要的逻辑。

## 新增方法注释规则

每添加一个新方法，都需要在方法签名前添加中文注释，说明该方法的作用和参数含义。

示例：

```csharp
/// <summary>
/// 初始化玩家移动状态。
/// </summary>
/// <param name="speed">玩家移动速度。</param>
/// <param name="direction">玩家移动方向。</param>
private void InitMoveState(float speed, Vector3 direction)
{
    // 方法实现
}
```

如果新增方法没有参数，也需要说明方法作用。

示例：

```csharp
/// <summary>
/// 刷新当前角色的动画状态。
/// </summary>
private void RefreshAnimationState()
{
    // 方法实现
}
```

## 回答与修改说明规则

1. 修改代码前，应先判断是否真的需要修改文件。
2. 修改代码时，应优先保持原有代码风格。
3. 修改完成后，需要简要说明：

   * 修改了哪些文件；
   * 改动了什么；
   * 是否需要用户在 Unity Inspector 中手动拖拽绑定变量。
4. 如果代码中新增了 `[SerializeField]` 字段，必须提醒用户回到 Unity Inspector 中完成引用绑定。
5. 修改 Unity 项目相关代码时，只在最终回答的最前方添加当前北京时间，格式建议为：`[北京时间：YYYY-MM-DD HH:mm]`,不是每次回答都添加，也不是第一次回答添加
