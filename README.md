# 🧠 EduMind

**一个 AI 驱动的智能学习助手 | An AI-powered learning assistant**

EduMind 结合现代化的 Fluent UI 设计、本地大语言模型 (Ollama) 和智能对话系统，让学习变得更聪明、更高效。用户可以随时提问，获得即时解答，并追踪自己的学习进度。

---

## 💻 技术栈 | Tech Stack

<p align="left">
    <img alt="C#" src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" />
    <img alt=".NET" src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
    <img alt="Avalonia" src="https://img.shields.io/badge/Avalonia-8A2BE4?style=for-the-badge&logo=avalonia&logoColor=white" />
    <img alt="FluentAvalonia" src="https://img.shields.io/badge/FluentAvalonia-0078D4?style=for-the-badge&logo=fluent&logoColor=white" />
    <img alt="MVVM" src="https://img.shields.io/badge/MVVM-CommunityToolkit-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
    <img alt="Ollama" src="https://img.shields.io/badge/Ollama-000000?style=for-the-badge&logo=ollama&logoColor=white" />
    <img alt="Qwen" src="https://img.shields.io/badge/Qwen-FF6B6B?style=for-the-badge&logo=alibaba&logoColor=white" />
</p>

---

## ✨ 已完成功能 | Completed Features

### 🤖 AI 智能对话 | AI Chat
- ✅ 接入本地 Ollama 服务，支持 Qwen3.5:9b / Qwen2.5:3b 等模型
- ✅ 流式输出，实时显示 AI 回复
- ✅ 消息气泡 + 时间戳，界面清爽
- ✅ 一键复制对话内容
- ✅ 自动滚动到最新消息

### 📁 文件总结 | File Summary
- ✅ 支持 PDF、Word、TXT、MD 文件上传
- ✅ 图片 OCR 文字识别（PNG/JPG）
- ✅ 实时进度条显示
- ✅ AI 自动提取要点生成摘要
- ✅ 一键复制总结结果
- ✅ 支持拖拽文件上传

### 📊 学习历史 | Learning History
- ✅ 自动保存所有对话记录（JSON 格式）
- ✅ 历史列表按日期分组（今日/昨天/更早）
- ✅ 实时搜索历史会话
- ✅ 点击加载历史对话继续聊天
- ✅ 右键菜单删除历史会话

### 📈 学习报告 | Learning Report
- ✅ 自定义报告周期（今天/本周/本月/最近7天/最近30天）
- ✅ AI 深度分析学习数据
- ✅ 统计知识点关注频率
- ✅ 生成个性化学习建议
- ✅ 导出 Word 格式学习报告
- ✅ 报告列表管理（打开/删除）

### 🎤 语音输入 | Voice Input
- ✅ 麦克风实时录音
- ✅ 语音自动转文字

### 🎨 界面设计 | UI Design
- ✅ Fluent 设计语言
- ✅ 悬停动画
- ✅ 可拖动无边框窗口

---

## 🚀 快速开始 | Quick Start

### 环境要求 | Prerequisites
- .NET 8.0 SDK 或更高版本
- [Ollama](https://ollama.ai/) 本地 AI 服务
- 推荐模型：`qwen2.5:3b`（速度快）或 `qwen3.5:9b`（效果更好）

### 安装步骤 | Installation

1. **克隆项目**
```bash
git clone https://github.com/yourusername/EduMind.git
cd EduMind
