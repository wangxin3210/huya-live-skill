# 虎牙直播 F11 全屏脚本
# 将浏览器窗口移到屏幕2并发送 F11

$skillPath = $PSScriptRoot
$csFile = Join-Path $skillPath "HuyaHelper.cs"

# 修复行尾问题（LF -> 统一处理）
$content = [System.IO.File]::ReadAllText($csFile)
$content = $content.Replace("`r`n", "`n")
[System.IO.File]::WriteAllText($csFile, $content, [System.Text.Encoding]::UTF8)

# 编译并执行
Add-Type -Path $csFile -ReferencedAssemblies System.Windows.Forms,System.Drawing
[HuyaHelper]::F11()
