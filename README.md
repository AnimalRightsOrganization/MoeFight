# MoeFight

## CS共享项目设置

1. VS → 工具 → 选项 → [搜索Unity]
	a. → 访问项目属性 False → True，使解决方案右键可显示属性。
	b. → 禁止完整生成项目 True → False，使编译Unity时生成dll（正常项目速度变慢）。

2. 所有引用项的复制本地 True → False。

## 模拟弱网

clumsy 延迟

``udp and outbound and ip.DstAddr = 8.8.8.8``