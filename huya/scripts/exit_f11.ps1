# 虎牙直播退出 F11 全屏脚本

# 将鼠标移到屏幕2
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(2500, 500)
Start-Sleep -Milliseconds 500

# 发送 F11 键退出全屏
$wshell = New-Object -ComObject wscript.shell
$wshell.SendKeys('{F11}')
