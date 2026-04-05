---
name: huya
description: |
  虎牙直播观看优化：全屏 + 最高画质。自动将虎牙直播间调到蓝光8M最高画质并双层全屏（F11 + CSS强制铺满）。
  触发词：虎牙全屏、虎牙画质、优化直播、看直播、huya fullscreen、huya optimize 等。
---

# Huya 虎牙直播优化

一键完成虎牙直播的最高画质 + 全屏铺满。已验证可用。

## 前置条件

- Chrome/Edge 浏览器已启用远程调试，MCP（mcp-chrome）已连接可用
- 用户常在 Chrome 中保持虎牙网站登录态

## 多显示器布局

```
屏幕1 (Primary): X=0,    1920x1080
屏幕3:            X=1920, 1920x1080
屏幕2:            X=3840, 1920x1080  ← 浏览器全屏目标
```

## MCP 连接优先级

1. **优先使用 `mcp-chrome`**（chrome_navigate / chrome_javascript / chrome_computer），已验证稳定可用
2. `chrome-devtools` 如果可用也可以用（list_pages / navigate_page / evaluate_script / take_snapshot）
3. 如果两个 MCP 都不通，提示用户检查 Chrome 远程调试是否开启

---

## 完整提示词（用户可直接复制使用）

```
打开 https://www.huya.com/<房间号>
用 Chrome MCP 打开这个页面。先 F11 让浏览器全屏，F11 的时候把鼠标聚焦在屏幕2，否则无法实现浏览器全屏。
参考 huya 的 skill，使直播画面全屏，然后选择最高画质，选择画质的按钮也是悬停才能出现，他在全屏那个按钮的左侧。
如果画质选择按钮的二级菜单里面只有一个选项，就选默认。如果有更多，最高画质选择蓝光8M，如果没有这个选项，请选择次一级的画质。并且提醒我。
```

---

## 执行流程（4 步，每步有跳过检查）

### Step 0：打开直播间页面

使用 `mcp-chrome` 的 `chrome_navigate` 打开指定虎牙直播间 URL。

```javascript
// mcp-chrome 调用示例
chrome_navigate({ url: "https://www.huya.com/98287" })
```

等待 5 秒让页面完全加载（`chrome_computer({ action: "wait", duration: 5 })`），然后截图确认。

### Step 1：F11 浏览器全屏

**⚠️ 关键点**：F11 时鼠标必须聚焦在屏幕2，否则全屏会跑到屏幕1。HuyaHelper.cs 通过 Win32 API 将窗口移动到屏幕2(X=3840)后再发送 F11，解决了这个问题。

先检查是否已全屏，已全屏则跳过。

**检查**：
```javascript
JSON.stringify({
  isF11: window.outerWidth === screen.availWidth && window.outerHeight >= screen.availHeight - 10,
  innerW: window.innerWidth, innerH: window.innerHeight,
  screenW: screen.width, screenH: screen.height
})
// 成功全屏时 innerW=1920, innerH=1080（屏幕2）
```

**执行**（未全屏时）：

创建 PowerShell 脚本文件 `do_f11.ps1`，然后执行：

```powershell
# do_f11.ps1 内容
$csFile = "C:\Users\admin\.workbuddy\skills\huya\scripts\HuyaHelper.cs"
$content = [System.IO.File]::ReadAllText($csFile)
$content = $content.Replace("`r`n", "`n")
[System.IO.File]::WriteAllText($csFile, $content, [System.Text.Encoding]::UTF8)
Add-Type -Path $csFile -ReferencedAssemblies System.Windows.Forms,System.Drawing
[HuyaHelper]::F11()
```

执行命令：
```
powershell -ExecutionPolicy Bypass -File "C:\Users\admin\.workbuddy\skills\huya\scripts\do_f11.ps1"
```

等待 2 秒后重新检查全屏状态。

### Step 2：CSS 强制视频铺满视口

CDP/MCP 环境无法调用 Fullscreen API（权限检查失败），用注入 CSS 替代视频元素全屏。

先检查是否已注入：
```javascript
!!document.getElementById('force-fullscreen-style')
```

**执行**（未注入时）：
```javascript
const s = document.createElement('style');
s.id = 'force-fullscreen-style';
s.textContent = `
  #J_playerMain,.room-player-wrap,.room-player-layer,.room-player,
  #videoContainer,#player-wrap,#player-video,.player-video {
    position:fixed!important; top:0!important; left:0!important;
    width:100vw!important; height:100vh!important;
    z-index:999999!important; margin:0!important; padding:0!important;
  }
  video#hy-video {
    width:100vw!important; height:100vh!important;
    object-fit:contain!important;
  }
  .player-bar-wrap,.player-controls {
    position:fixed!important; bottom:0!important;
    z-index:9999999!important;
  }
`;
document.head.appendChild(s);
return 'CSS injected';
```

**验证**：
```javascript
const v = document.querySelector('video#hy-video');
const r = v.getBoundingClientRect();
JSON.stringify({w: Math.round(r.width), h: Math.round(r.height), ok: r.width >= window.innerWidth && r.height >= window.innerHeight});
// 成功时 ok=true，w/h 约等于视口尺寸
```

### Step 3：画质调到最高

**画质选择逻辑**：
1. 如果二级菜单只有 1 个选项 → 选默认（当前画质就是唯一选项）
2. 如果有多个选项 → 优先选"蓝光8M"
3. 如果没有蓝光8M → 选次一级画质（蓝光4M > 超清 > 高清），**并提醒用户**

**检查当前画质**：
```javascript
const btn = document.querySelector('.player-videotype');
btn ? btn.textContent.trim().substring(0, 20) : 'not found';
// 返回如 "蓝光8M"、"蓝光4M"、"超清" 等
```

**执行**（不是最高画质时）：

1. 模拟 hover 打开画质面板（画质按钮在全屏按钮左侧，hover 才出现）：
```javascript
const btn = document.querySelector('.player-videotype');
if (btn) {
  btn.dispatchEvent(new MouseEvent('mouseenter', {bubbles:true, cancelable:true}));
  btn.dispatchEvent(new MouseEvent('mouseover', {bubbles:true, cancelable:true}));
  return 'hovered on videotype button';
}
```

2. 等 1 秒后查找画质选项：
```javascript
const items = document.querySelectorAll('.player-videotype-list li');
let result = [];
for (const item of items) {
  result.push(item.textContent.trim());
}
JSON.stringify({count: items.length, items: result});
// 返回如 {count:4, items:["蓝光8M","蓝光4M","超清","高清"]}
```

3. 根据选项数量选择画质：
```javascript
const items = document.querySelectorAll('.player-videotype-list li');
if (items.length <= 1) {
  // 只有一个选项，保持默认
  return 'only one option, keep default';
}
// 画质优先级顺序
const priority = ['蓝光8M', '蓝光4M', '超清', '高清', '标清'];
for (const target of priority) {
  for (const item of items) {
    if (item.textContent.trim().includes(target)) {
      item.dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true, view:window}));
      return target === '蓝光8M' ? 'clicked 蓝光8M (best)' : 'clicked ' + target + ' (提醒：非最高画质)';
    }
  }
}
```

4. 等 3 秒后验证画质：
```javascript
const btn = document.querySelector('.player-videotype');
btn ? btn.textContent.trim().substring(0, 20) : 'not found';
```

---

## MCP 调用方式备忘

### mcp-chrome（推荐）
- **导航**：`chrome_navigate({ url: "..." })`
- **截图**：`chrome_computer({ action: "screenshot" })`
- **等待**：`chrome_computer({ action: "wait", duration: N })`
- **执行JS**：`chrome_javascript({ code: "..." })`
- **读取页面**：`chrome_read_page()`

### chrome-devtools（备选）
- **导航**：`navigate_page({ type: "url", url: "..." })`
- **截图**：`take_screenshot()`
- **执行JS**：`evaluate_script({ expression: "..." })`
- **页面快照**：`take_snapshot()`

---

## 编译注意事项

- **`out _` 语法不支持**：C# 7+ 的 `out _` 在 PowerShell Add-Type 中编译失败，必须显式声明 `out uint pid;`
- **行尾问题**：`write_to_file` 生成的 LF 可能被 PowerShell 操作转为 CRLF，导致编译错误。执行前先 `$content.Replace("`r`n", "`n")`
- **PowerShell Here-String 中的 `&&`**：会被 PowerShell 错误解析。C# 代码**必须**用文件方式（`write_to_file` + `Add-Type -Path`），不要用 `Add-Type @"..."@`
- **PowerShell 脚本用 write_to_file 创建**：不要用 heredoc，Windows PowerShell 对 heredoc 支持不稳定。先 `write_to_file` 写 `.ps1` 文件，再用 `powershell -ExecutionPolicy Bypass -File` 执行

---

## 故障排除

| 问题 | 原因 | 解决 |
|------|------|------|
| F11 没反应 | 窗口不在前台 | `AttachThreadInput` + `SetForegroundWindow` 后再发 |
| F11 后窗口跳屏 | `SetForegroundWindow` 把窗口拉走 | `HuyaHelper.cs` 已内置 `MoveWindow` 到屏幕2 |
| F11 全屏到了屏幕1 | 鼠标不在屏幕2 | HuyaHelper.cs 通过 MoveWindow 先移到屏幕2再发 F11 |
| CSS 注入后视频没铺满 | 选择器不匹配 | 用 `document.querySelectorAll('[class*="player"]')` 检查实际 class |
| 画质面板打不开 | hover 事件没触发 | 同时 dispatch `mouseenter` + `mouseover` 两个事件 |
| Fullscreen API 权限失败 | MCP 调用不被视为用户手势 | 改用 CSS 方案（Step 2） |
| chrome-devtools MCP 报错 | 连接不稳定 | 切换到 mcp-chrome MCP |
