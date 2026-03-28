# 🧠 EduMind
一个 AI 驱动的智能学习助手 | An AI-powered learning assistant

EduMind 结合现代化的 Fluent UI 设计、本地大语言模型 (Ollama) 和智能对话系统，让学习变得更聪明、更高效。用户可以随时提问，获得即时解答，并追踪自己的学习进度。

# 💻 技术栈 | Tech Stack
<p align="left"> <img alt="C#" src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" /> <img alt=".NET" src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" /> <img alt="Avalonia" src="https://img.shields.io/badge/Avalonia-8A2BE4?style=for-the-badge&logo=avalonia&logoColor=white" /> <img alt="FluentAvalonia" src="https://img.shields.io/badge/FluentAvalonia-0078D4?style=for-the-badge&logo=fluent&logoColor=white" /> <img alt="MVVM" src="https://img.shields.io/badge/MVVM-CommunityToolkit-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" /> <img alt="Ollama" src="https://img.shields.io/badge/Ollama-000000?style=for-the-badge&logo=ollama&logoColor=white" /> <img alt="Qwen" src="https://img.shields.io/badge/Qwen-FF6B6B?style=for-the-badge&logo=alibaba&logoColor=white" /> <img alt="SQLite" src="https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white" /> </p>
# ✨ 核心功能 | Key Features
# 🤖 AI 智能对话 | AI Chat
接入本地 Ollama 服务，支持 Qwen3.5:9b 等模型

流式输出，实时显示 AI 回复

消息气泡 + 时间戳，界面清爽

一键复制对话内容

# 📁 文件总结 | File Summary
上传 PDF/Word/图片，AI 自动提取要点

视频/音频文件转文字 + 智能总结

支持拖拽上传，一键生成摘要

# 📊 学习追踪 | Learning Tracking
自动记录所有提问历史

按时间/学科分类查看

统计高频知识点

生成个人学习报告

# 🎨 主题切换 | Theme Switching
亮色/暗色模式一键切换

毛玻璃效果 + Fluent 设计语言

界面跟随主题自动适配

# 📁 项目结构 | Project Structure
text
EduMind/
├─ EduMind/                      # 主项目目录
│  ├─ Views/                      # 视图层
│  │  ├─ HomeView.axaml           # 聊天主界面
│  │  ├─ HistoryView.axaml        # 历史记录页面
│  │  └─ SettingsView.axaml       # 设置页面
│  ├─ ViewModels/                 # 视图模型
│  │  ├─ HomeViewModel.cs         # 聊天逻辑
│  │  ├─ HistoryViewModel.cs      # 历史记录逻辑
│  │  └─ SettingsViewModel.cs     # 设置逻辑
│  ├─ Models/                      # 数据模型
│  │  └─ ChatMessage.cs            # 聊天消息模型
│  ├─ Services/                    # 服务层
│  │  ├─ OllamaService.cs          # AI 服务
│  │  └─ FileService.cs            # 文件处理
│  ├─ Controls/                     # 自定义控件
│  │  └─ ExpanderStyles.axaml       # 样式资源
│  └─ Assets/                       # 资源文件
└─ README.md                        # 项目说明
# 🚀 快速开始 | Quick Start
环境要求 | Prerequisites
.NET 8.0 SDK

Ollama (用于本地 AI 服务)

Qwen3.5:9b 模型 (或其他 Ollama 支持的模型)

安装步骤 | Installation
克隆项目 | Clone the repository

bash
git clone https://github.com/yourusername/EduMind.git
cd EduMind
安装 Ollama 并下载模型 | Install Ollama and pull model

bash
# 安装 Ollama (访问官网下载)
# 下载 Qwen 模型
ollama pull qwen3.5:9b
# 或使用更小的模型（推荐）
ollama pull qwen2.5:3b
运行项目 | Run the project

bash
dotnet restore
dotnet build
dotnet run --project EduMind
⚙️ 配置 | Configuration
在 HomeViewModel.cs 中可以修改 Ollama 配置：

csharp
private const string OLLAMA_URL = "http://localhost:11434/api/chat";
private const string MODEL_NAME = "qwen3.5:9b";  // 可换成其他模型
# 🎯 使用指南 | Usage Guide
# 💬 开始对话 | Start Chatting
打开应用，进入首页

在输入框输入问题

按 Enter 或点击发送按钮

AI 会实时回复，并显示"正在回复..."提示

# 📤 上传文件 | Upload Files
点击输入框旁边的附件按钮

选择 PDF、Word、图片或视频文件

AI 自动提取内容并生成摘要

摘要会以对话形式展示

# 📈 查看历史 | View History
点击侧边导航栏的"历史"按钮

按日期查看所有提问记录

点击某条记录可查看完整对话

查看学习统计和分析图表

# 🎨 切换主题 | Switch Theme
进入设置页面

在"主题设置"中选择亮色/暗色/跟随系统

界面会自动切换主题

# 🔧 故障排除 | Troubleshooting
❌ AI 没有回复 | AI not responding
检查 Ollama 是否在运行：ollama list

确认模型已下载：ollama run qwen3.5:9b 测试

检查 URL 配置是否正确（默认 localhost:11434）

❌ 文件上传失败 | File upload failed
确认文件格式支持（PDF/Word/图片/视频）

文件大小限制：100MB

检查 Services/FileService.cs 中的路径配置

❌ 主题切换无效 | Theme switch not working
重启应用

检查 SettingsViewModel 中的主题保存逻辑

确认 App.axaml 中已配置 <FluentTheme />

# 🚀 演示技巧 | Demo Tips
准备几个预设问题：展示不同学科的知识问答

上传示例文件：准备一个 PDF 文档展示总结功能

切换主题：展示亮色/暗色模式的美观性

查看历史：展示学习进度追踪功能

现场提问：让评委现场提问，展示 AI 实时回复

# 📝 待办功能 | Roadmap
多会话管理（多个对话标签）

Markdown 渲染（让 AI 回复更美观）

语音输入（麦克风录制转文字）

学习报告导出（PDF/Word）

云端同步（多设备历史记录同步）

知识图谱可视化

# 📄 许可证 | License
MIT License © 2024 EduMind Team

# 🙏 致谢 | Acknowledgements
Avalonia UI - 跨平台 UI 框架

FluentAvalonia - Fluent 设计语言实现

CommunityToolkit.Mvvm - MVVM 工具包

Ollama - 本地大语言模型服务

Qwen - 通义千问模型

用 EduMind，让学习更聪明 | Learn Smarter with EduMind 🌟
