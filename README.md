# 🧠 EduMind

**一个 AI 驱动的智能学习助手 | An AI-powered learning assistant**

EduMind 结合现代化的 Fluent UI 设计、本地大语言模型 (Ollama) 和智能对话系统，让学习变得更聪明、更高效。用户可以随时提问，获得即时解答，并追踪自己的学习进度。

---

## 💻 技术栈 | Tech Stack

- C# / .NET 8.0
- Avalonia UI (跨平台 UI 框架)
- FluentAvalonia (Fluent 设计语言)
- CommunityToolkit.Mvvm (MVVM 工具包)
- Ollama (本地大语言模型)
- Qwen3.5:9b / Qwen2.5:3b (AI 模型)

---

## ✨ 已完成功能 | Completed Features

### 🤖 AI 智能对话
- ✅ 接入本地 Ollama 服务，支持 Qwen3.5:9b / Qwen2.5:3b 等模型
- ✅ 流式输出，实时显示 AI 回复
- ✅ 消息气泡 + 时间戳，界面清爽
- ✅ 一键复制对话内容
- ✅ 自动滚动到最新消息

### 📁 文件总结
- ✅ 支持 PDF、Word、TXT、MD 文件上传
- ✅ 图片 OCR 文字识别（PNG/JPG）
- ✅ 实时进度条显示
- ✅ AI 自动提取要点生成摘要
- ✅ 一键复制总结结果
- ✅ 支持拖拽文件上传

### 📊 学习历史
- ✅ 自动保存所有对话记录（JSON 格式）
- ✅ 历史列表按日期分组（今日/昨天/更早）
- ✅ 实时搜索历史会话
- ✅ 点击加载历史对话继续聊天
- ✅ 右键菜单删除历史会话

### 📈 学习报告
- ✅ 自定义报告周期（今天/本周/本月/最近7天/最近30天）
- ✅ AI 深度分析学习数据
- ✅ 统计知识点关注频率
- ✅ 生成个性化学习建议
- ✅ 导出 Word 格式学习报告
- ✅ 报告列表管理（打开/删除）

### 🎤 语音输入
- ✅ 麦克风实时录音
- ✅ 语音自动转文字

### 🎨 界面设计
- ✅ Fluent 设计语言
- ✅ 悬停动画
- ✅ 可拖动无边框窗口

---

## 🚀 快速开始 | Quick Start

### 环境要求
- .NET 8.0 SDK 或更高版本
- [Ollama](https://ollama.ai/) 本地 AI 服务
- 推荐模型：`qwen2.5:3b`（速度快）或 `qwen3.5:9b`（效果更好）

### 安装步骤

**1. 克隆项目**
```bash
git clone https://github.com/yourusername/EduMind.git
cd EduMind
```

### 下载 AI 模型
```bash
ollama pull
 qwen2.5:3b
```
### 3. 运行项目
```bash
dotnet restore
dotnet run --project EduMind
```
```bash
git clone https://github.com/yourusername/EduMind.git
cd EduMind
```

## 🎯 使用指南 | Usage Guide

###💬 开始对话
- 打开应用进入聊天界面
- 在输入框输入问题
- 点击发送按钮
- AI 实时回复

###📤 总结文件
- 点击输入框旁的附件按钮
- 选择 PDF、Word、图片等文件
- 等待 AI 提取内容并生成摘要
- 总结结果以对话形式展示

###📊  生成报告
- 进入"学习报告"页面
- 选择报告周期
- 点击"生成报告"
- 系统自动生成 Word 学习报告并保存

### 📝 待办功能 | Roadmap
- 多会话管理
- 云端同步
- 学习报告图表可视化
- Markdown 渲染支持

### 📄 许可证 | License
MIT License © 2025 EduMind Team

### 🙏 致谢 | Acknowledgements
- Avalonia UI - 跨平台 UI 框架
- FluentAvalonia - Fluent 设计语言实现
- CommunityToolkit.Mvvm - MVVM 工具包
- Ollama - 本地大语言模型服务
- Qwen - 通义千问模型
