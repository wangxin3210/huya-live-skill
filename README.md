# 虎牙直播观看优化 Skill

一键完成虎牙直播的最高画质 + 全屏铺满。

## 功能

- 🖥️ **浏览器 F11 全屏**（支持多显示器，自动定位到指定屏幕）
- 📺 **CSS 强制视频铺满**（双层全屏效果）
- 🎬 **自动选择最高画质**（优先蓝光8M，其次蓝光4M/超清/高清）

## 安装

1. 将整个 `huya` 文件夹复制到你的 WorkBuddy skills 目录：
   - Windows: `%USERPROFILE%\.workbuddy\skills\`
   - macOS/Linux: `~/.workbuddy/skills/`

2. 确保 Chrome 已启用远程调试，且 MCP（mcp-chrome）已连接

## 使用方法

直接对 AI 说：
```
打开 https://www.huya.com/<房间号>
用 Chrome MCP 打开这个页面。先 F11 让浏览器全屏，F11 的时候把鼠标聚焦在屏幕2。
参考 huya 的 skill，使直播画面全屏，然后选择最高画质。
```

## 前置条件

- Chrome/Edge 浏览器已启用远程调试
- mcp-chrome MCP 已连接可用

## 文件结构

```
huya/
├── SKILL.md          # Skill 定义和完整使用说明
└── scripts/
    ├── HuyaHelper.cs     # C# 辅助类（窗口操作）
    ├── do_f11.ps1        # 进入全屏脚本
    └── exit_f11.ps1      # 退出全屏脚本
```

## 多显示器配置

默认配置（3屏幕）：
- 屏幕1 (Primary): X=0, 1920x1080
- 屏幕3: X=1920, 1920x1080
- 屏幕2: X=3840, 1920x1080 ← 浏览器全屏目标

如需修改，编辑 `scripts/HuyaHelper.cs` 中的 `SCREEN2_X` 值。

## 许可证

MIT