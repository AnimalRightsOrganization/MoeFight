# MoeFight

## CS共享项目设置

1. VS → 工具 → 选项 → [搜索Unity]
	a. → 访问项目属性 False → True，使解决方案右键可显示属性。
	b. → 禁止完整生成项目 True → False，使编译Unity时生成dll（正常项目速度变慢）。
2. 所有引用项的复制本地 True → False。

## 模拟弱网

clumsy 延迟

``udp and outbound and ip.DstAddr = 192.168.1.101``



- [GGPO回滚演示](https://www.bilibili.com/video/BV1Gf4y1A7XT)



## 规则

- 时长
  - 比赛90秒
  - 训练无限∞
- 帧率
  - 60
- 暂停
	- 每局1次，每次30秒。
- 断线重连
	- PING > 400ms，被服务器踢回大厅。提示调整网络或弃权。
	- 断线后，60秒内重连，超时判负。
	- 房间内等待的玩家可解散比赛，不计分。

## 技能

- AOI
  - ↓← + U
  - →↓ + I