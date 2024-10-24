# Wukong_PBData_ReadWriter_GUI

## 功能描述

### 解包PBTable里的.data
可以将所有游戏解包出来的protobuf .data文件可视化展示

### .data编辑
可视化编辑 .data文件 并保存成新文件

### 生成PAK
支持以文件夹拖拽的形式，将文件夹打包成游戏内可用的pak mod文件

假如修改后的data文件路径是 \b1\Content\00Main\PBTable\Runtime\HuluDesc.data
那么在生成pak时，你需要在这个路径加上你的mod名文件夹，假如你mod命名为MyMod
完整路径为 MyMod\b1\Content\00Main\PBTable\Runtime\HuluDesc.data
将 MyMod 文件夹拖放进生成pak格式的窗口内即可完成打包

### 备注 导入及导出
可以自己对 .data的数据进行备注，以便于标记 已经分析过的配表
支持导入他们配置好的备注文件，以便于更多的mod作者之间 协作分析全量配表

## 使用方法

打开软件后，菜单栏 文件 -> 打开Data目录。 选择一个含有.data文件（一般使用 FModel解包出来的 PBTable文件夹）的文件夹打开即可

从做到右分块 依次为 data文件列表， 单个data文件内的数据列表， 单个数据内的 每个字段数据显示

.data为 protobuf文件，每个数据内 个别字段是proto嵌套形式。目前以 打开新窗体的方式进行嵌套数据编辑

其余上述功能均在 菜单栏 文件 内

## 注意事项

1. 这个工具只是data文件的编辑器，并不支持打开其余FModel解开的uasset文件
2. 本工具使用.net 8.0，如果需要使用请安装.net8.0库
3. 如果有大佬觉得工具写的不好，可以自行clone进行修改。二次发布标注作者引用即可
4. 在编辑器内切换左侧data数据列表前，请一定要先保存数据，切换后之前修改的data文件记录将被清空
5. 如果有对工具有任何疑问，或者有大佬想要一起协作开发，可以联系作者：[禽兽-云轩](https://space.bilibili.com/8729996)

## 后续更新计划

1. 支持多文件修改保存(已完成)
2. 支持Undo Redo
3. dirty 的data文件标记（已完成）
4. data pak的解包及merge （解包完成）
5. 新增数据（已完成）
