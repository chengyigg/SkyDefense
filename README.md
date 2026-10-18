✈️ SkyDefense - 3D 联机合作塔防游戏

SkyDefense 是一款基于 Unity 3D 开发的俯视角双人合作塔防游戏。玩家扮演指挥官驾驶飞机在地图上穿梭，通过实地考察来建造、升级防御塔，并配合战术技能共同抵御敌人的进攻。

本项目不仅实现了完整的单机塔防闭环，还深度集成了 Netcode for GameObjects (NGO)，实现了基于 Host-Client 架构的双人局域网/互联网联机体验。同时，项目在底层架构上进行了大量的性能优化与工程化封装。

📦 下载可玩版本（Windows） 

✨ 核心特性 (Features)
🎮 玩家与操作
网格化移动：鼠标点击地面，飞机平滑转向并移动至 5x5 网格中心，配有红绿变色幽灵预览方块（合法为绿，非法为红）。

目标锁定：左键点击敌人触发锁定（红圈缩放动画），所有防御塔将优先攻击被锁定的目标。

战术技能：按 Q 键消耗金币召唤 3 架僚机，围绕目标环绕倾泻火力（带完善的 UI 扇形冷却遮罩）。

🏰 塔防系统
四种防御塔：

机枪塔：高射速单体伤害。

减速塔：释放扩散脉冲，挂载减速与定身 Debuff。

群伤塔：迫击炮抛物线射击，落地造成范围爆炸。

狙击塔：超远射程，带激光瞄准线蓄力，高单体爆发（开火附带顿帧打击感）。

升级与回收：所有塔支持 Lv1 - Lv3 升级，属性动态成长，支持 70% 金币返还的卖塔功能（附带建造/拆除扬尘特效）。

波次管理：多波次生成，支持波次提示、金币经济、基地血量扣减及胜负结算。

🌐 联机同步 (Networking)
状态同步：基于 NetworkVariable 实时同步金币、血量、波次进度与游戏状态。

防作弊校验：建造、升级、卖塔全部通过 ServerRpc 发送至主机，并在服务端进行网格对齐与玩家距离合法性双重校验，杜绝改包刷钱与远程建塔。

表现分离：子弹、爆炸、脉冲等视觉特效通过 ClientRpc 广播，确保双端表现一致。

独立控制：基于自定义的 OwnerNetworkTransform 实现客户端权威的玩家移动，保证操作手感。

🛠️ 技术栈与架构 (Tech Stack)
引擎：Unity 2022.3 LTS (URP)

网络库：Unity Netcode for GameObjects (NGO), Unity Transport

数据驱动：使用 ScriptableObject (TowerData) 管理塔的配置与成长曲线。

设计模式：单例 (GameManager等)、观察者模式 (UI事件解耦)、策略模式 (塔的攻击逻辑与索敌优先级)。

🚀 深度性能优化 (Performance Optimizations)
本项目使用了大量高级性能优化手段，确保在同屏 200+ 敌人与密集特效下依然保持稳定帧率（建议在此处附带 Profiler 前后对比截图）。

材质合批 (MaterialPropertyBlock)：重构了敌人的材质渲染逻辑，摒弃传统的 .material 克隆，改用共享材质与 MaterialPropertyBlock 单独控制受击/减速颜色。将 Draw Call 从 O(N) 降至 O(1)。

空间哈希网格 (Spatial Hash Grid)：重构了索敌算法。为敌方单位建立动态网格，塔的索敌查询复杂度从 O(N) 降至近乎 O(1)，极大缓解了多塔齐射时的 CPU 压力。

零 GC 对象池 (Object Pool)：针对高频生成的子弹、迫击炮弹、伤害飘字，手写了专用的对象池，彻底消除因 Instantiate/Destroy 带来的 GC 卡顿。

UI 渲染优化：废弃了每个敌人生成两个 SpriteRenderer 血条的落后方案，改为统一在一个 World Space Canvas 下管理，将 400+ 的 UI Draw Call 降至 2 个以内。

网络带宽优化：将高频触发的受击/死亡音效，从 RPC 广播重构为监听 NetworkVariable.OnValueChanged 本地触发，大幅节省网络带宽。

打击感与反馈 (Game Feel)：引入了客户端受击闪白（Hit Flash）、狙击塔专属顿帧（Hit Stop）、以及基于对象池的 3D 伤害飘字。

🧰 自研编辑器工具 (Custom Editor Tools)
为提升开发与排查效率，项目内编写了 7 个定制化 Editor 工具：

脚本/资源导出器：一键提取项目中所有脚本或资源路径，解决编码与备份问题。

预制体结构查看器：深度检查 Prefab 的层级、RectTransform 锚点及 Image 组件（直击 UI 拼装痛点）。

场景结构体检器：一键扫描场景内所有敌人、塔、拖尾和材质的状态，排查丢失引用。

材质属性查看器：用于快速定位复杂 Shader（如 glTF 材质）的属性名，解决代码变色失效问题。

一键挂载 UI 音效：自动扫描场景内所有 Button 并挂载音效脚本。

🎮 如何运行 (How to Run)
1. 单人模式
在 Unity 中打开 Assets/Scenes/SampleScene 场景。

点击 Play。

在主菜单点击 “单人模式”。

2. 双人联机测试
本项目使用 ParrelSync 进行多开测试。

确保已通过 Package Manager 导入 ParrelSync。

点击菜单栏 ParrelSync -> Clones Manager -> Create new clone。

Editor 窗口 A：点击 Play，在主菜单选择 “多人游戏” -> “作为主机”。

Editor 窗口 B (Clone)：打开克隆项目并 Play，在主菜单选择 “多人游戏” -> “加入游戏”。

输入 127.0.0.1 点击连接即可同步游玩。

注：跨网络联机建议使用 ZeroTier 或 Tailscale 等虚拟局域网工具组网。

开发者：石诚意
联系方式：3201663235@qq.com
