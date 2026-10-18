SkyDefense 技术设计文档
Version 1.0 · 2026-10-18 · 石诚意

1. 项目概述
项目名称：SkyDefense (3D 联机合作塔防)

引擎版本：Unity 2022.3 LTS (Universal Render Pipeline)

网络框架：Unity Netcode for GameObjects (NGO)

项目定位：以双人局域网/广域网合作为核心，具备高完成度、高帧率、高度工程化的 3D 塔防 Demo。

2. 系统架构设计 (System Architecture)
项目采用分层架构与单例驱动的设计模式，确保各系统之间的低耦合与高内聚。

管理层 (Manager Layer)：GameManager (全局状态)、BuildManager (建造逻辑)、EnemyManager (敌人中枢)、SoundManager (音频池)、HealthBarManager (UI血条)。

实体层 (Entity Layer)：Player (玩家飞机)、Enemy (敌人)、Tower (防御塔)、Bullet / MortarShell (弹道)。

数据层 (Data Layer)：基于 ScriptableObject 的 TowerData，剥离数值配置与逻辑代码，支持策划独立调整成长曲线。

表现层 (View Layer)：UIPanelAnimator (UI动画)、DamageTextManager (飘字池)、CameraShake (震屏)。

3. 网络同步架构设计 (Networking Design)
联机机制基于 Host-Client 架构，严格按照“状态同步 + 操作校验 + 表现分离”的原则开发。

3.1 网络同步策略
状态同步 (NetworkVariable)：金币、血量、波次、游戏状态等全局数据，以及敌人的血量、减速状态，均使用 NetworkVariable。客户端通过 OnValueChanged 监听状态变化更新 UI，零 RPC 带宽开销。

表现分离 (ClientRpc)：子弹生成、爆炸特效、音效等纯视觉表现，通过 ClientRpc 广播（或基于 NetworkVariable 的变化本地触发）。

客户端权威 (Owner Authoritative)：玩家飞机的移动使用自定义的 OwnerNetworkTransform，消除了网络延迟带来的操作粘滞感，保证 PvE 游戏的操作手感。

3.2 服务端防作弊校验 (Anti-Cheat)
所有经济与建造操作（建塔、升级、卖塔）均通过 ServerRpc 发送给主机。
服务端在扣除金币前，执行以下校验逻辑：

网格对齐校验：验证请求的 buildPos 是否严格对齐到 5x5 网格。

距离校验：验证建造位置与玩家实际位置的距离是否超出合法交互半径，防止远程建塔。

4. 核心系统详细设计 (Core Systems)
4.1 索敌与空间优化 (Spatial Hash Grid)
痛点：早期版本塔的索敌采用遍历全量敌人列表（O(N)），当同屏敌人超过 100 时，CPU 压力剧增。

方案：在 EnemyManager 中引入空间哈希网格。将地图划分为 10x10 单位的网格。

收益：塔在索敌时，只需查询自身周围的 9 个网格内的敌人，时间复杂度降至 O(1)。配合 0.15 秒的分帧网格重建机制，CPU 耗时降低 80% 以上。

4.2 对象池系统 (Object Pool)
痛点：子弹、迫击炮、飘字高频生成与销毁，产生大量 GC (Garbage Collection) 导致帧率卡顿。

方案：手写基于 Queue<T> 的专用对象池 (Bullet.SpawnFromPool / MortarShell.SpawnFromPool)。

收益：复用 GameObject，实现高频战斗场景下的 GC Alloc 0 开销。

4.3 战斗与打击感系统 (Game Feel)
受击闪白 (Hit Flash)：结合 MaterialPropertyBlock，敌人受击瞬间材质强制替换为白色，0.08 秒后恢复，提供极强的命中确认感。

顿帧 (Hit Stop)：狙击塔等高爆发塔开火时，在客户端本地通过协程暂缓 Time.timeScale 0.05 秒，提升视觉冲击力。

伤害飘字 (Damage Text)：基于 UGUI 和 TMP，使用对象池管理。在 3D 世界坐标与世界坐标间进行转换，并利用随机偏移与向上插值，避免文字重叠。

5. 渲染与性能优化设计 (Performance Optimizations)
5.1 材质合批优化 (MaterialPropertyBlock)
痛点：早期读取敌人 MeshRenderer.material 会导致材质实例化，200 个敌人产生 200 份材质，Dynamic Batching 完全失效，Draw Call 爆炸。

方案：改用 sharedMaterial，在受击/减速需要变色时，使用 MaterialPropertyBlock 注入颜色属性。

收益：同屏所有敌人共享 1 份材质，Draw Call 稳定在个位数。

5.2 UI 渲染重构
痛点：每个敌人头顶 2 个 SpriteRenderer 组装血条，200 个敌人产生 400 个渲染批次。

方案：重构为全局唯一的 World Space Canvas (HealthBarCanvas)，统一管理所有血条，使用 Image.fillAmount 控制血量。

收益：UI 渲染批次从 400+ 降至 1-2 个 Batch。

5.3 网络带宽与事件驱动优化
优化：将受击、死亡等高频触发的音效，从 ClientRpc 广播重构为监听 NetworkVariable.OnValueChanged 事件本地播放，彻底解决网络风暴，节省带宽。

6. 编辑器工具链 (Editor Tools)
为提升开发与排查效率，开发了 7 个定制化 Editor 工具：

预制体结构查看器：深度检查 Prefab 的层级、RectTransform 与 Image 填充模式。

场景体检器：一键扫描场景内的敌人状态、材质丢失情况。

材质属性查看器：快速定位 Shader 属性名（解决 glTF 材质代码变色失效）。

一键 UI 音效挂载：通过反射自动扫描场景内所有 Button 并绑定音效。

脚本/资源导出器：一键提取项目内所有脚本路径，解决多端同步与备份问题。

7. 待扩展方向 (Future Work)
数据驱动关卡：将波次配置升级为 ScriptableObject 资产，支持策划纯配表生成不同关卡。

客户端预测与回滚 (Client Prediction & Rollback)：当前移动为客户端权威，若未来转为 PvP 竞技，将引入 CSP (Client-Side Prediction) 保证手感与公平性。

怪物种类扩展：增加飞行怪、护盾怪、Boss 等，利用策略模式拆分怪物的 AI 逻辑。

文档结束
