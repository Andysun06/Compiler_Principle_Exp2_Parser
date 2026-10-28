# ParseLab —— 《编译原理》实验二：语法分析

广州大学《编译原理》探索性实验（二）。用 C# / .NET 9 / WinForms 实现的语法分析实验系统，
覆盖 LL(1) 与 LR(0) / SLR(1) 两类分析方法，并自动绘制 LR(0) 项目集规范族的状态转换图。

## 实验内容

| 部分 | 说明 |
| --- | --- |
| 基础实验 LL(1) | 输入任意文法，自动消除左递归（直接与间接）、提取左公因子；计算 FIRST、FOLLOW、SELECT 集；构造 LL(1) 分析表，并用分析栈模拟识别过程，逐步输出「已匹配前缀 / 剩余输入串 / 所用产生式」 |
| 扩展实验 LR(0) | 拓广文法后由 CLOSURE / GOTO 求项目集规范族，构造 LR(0) ACTION / GOTO 表；自动检出移进-归约与归约-归约冲突，并绘制状态转换图 |
| 扩展实验 SLR(1) | 归约项只用 FOLLOW 集填充，消去 LR(0) 中不影响 SLR 判定的冲突，并给出冲突对比 |
| 通用能力 | 任意文法输入、四元组展示（Vn / Vt / S / P）、待识别字符串、内置示例、结果导出 |

## 目录结构

```
.
├── src/ParseLab/          源码（仅 3 个文件）
│   ├── Core.cs            文法变换、FIRST/FOLLOW/SELECT、LL(1) 与 LR/SLR 分析表构造
│   ├── UI.cs              全部界面（代码构建，不使用设计器），含状态转换图绘制
│   └── ParseLab.csproj    工程文件（已配置单文件自包含发布）
├── samples/               示例文法（5 个）
├── output/                程序运行产物（分析表、分析过程、状态转换图）
└── README.md
```

## 构建与运行

环境要求：Windows 10/11 + [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

```powershell
cd src/ParseLab
dotnet run
```

发布为单文件 exe（.NET 运行时一并打包，目标机无需安装任何环境）：

```powershell
cd src/ParseLab
dotnet publish -c Release
# 产物：bin/Release/net9.0-windows/win-x64/publish/ParseLab.exe
```

## 使用说明

- 左侧输入文法（每行一条产生式，右部可用 `|` 分隔多个候选式），程序自动推导非终结符、终结符、开始符号与产生式集合。
- 右侧分「LL(1) 分析」与「LR / SLR 分析」两大页，各自含分析表、分析过程、项目集规范族等子页。
- 「内置示例」下拉框可载入教材与指导书中的经典文法；下方输入待识别的字符串后即可逐表查看分析过程。

## 说明

广州大学《编译原理》探索性实验（二）作业源码。实验报告请见提交的文档。