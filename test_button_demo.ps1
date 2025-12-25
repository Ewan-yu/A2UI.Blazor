# 测试按钮演示功能的 PowerShell 脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "A2UI Blazor - 按钮演示测试脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 检查项目是否存在
$projectPath = "samples\A2UI.Sample.BlazorServer\A2UI.Sample.BlazorServer.csproj"
if (-not (Test-Path $projectPath)) {
    Write-Host "❌ 错误: 找不到项目文件 $projectPath" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 项目文件存在" -ForegroundColor Green

# 2. 停止可能正在运行的进程
Write-Host ""
Write-Host "正在停止可能正在运行的实例..." -ForegroundColor Yellow
Get-Process -Name "A2UI.Sample.BlazorServer" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "✓ 清理完成" -ForegroundColor Green

# 3. 编译项目
Write-Host ""
Write-Host "正在编译项目..." -ForegroundColor Yellow
Push-Location "samples\A2UI.Sample.BlazorServer"
$buildResult = dotnet build 2>&1
Pop-Location

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 编译失败!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "✓ 编译成功" -ForegroundColor Green

# 4. 启动应用
Write-Host ""
Write-Host "正在启动应用..." -ForegroundColor Yellow
Write-Host "提示: 应用将在后台运行。请在浏览器中测试功能。" -ForegroundColor Cyan
Write-Host ""
Write-Host "测试步骤:" -ForegroundColor Yellow
Write-Host "  1. 浏览器会自动打开（或手动访问 http://localhost:5000）" -ForegroundColor White
Write-Host "  2. 点击页面顶部的快捷按钮: 🔘 显示按钮" -ForegroundColor White
Write-Host "  3. 检查是否显示包含两个按钮的卡片：" -ForegroundColor White
Write-Host "     - 标题: '交互按钮演示'" -ForegroundColor White
Write-Host "     - 描述: '点击按钮与 Agent 交互：'" -ForegroundColor White
Write-Host "     - 按钮: '👍 喜欢' 和 '🔗 分享'" -ForegroundColor White
Write-Host "  4. 点击按钮测试交互功能" -ForegroundColor White
Write-Host ""
Write-Host "按 Ctrl+C 停止应用" -ForegroundColor Cyan
Write-Host ""

Push-Location "samples\A2UI.Sample.BlazorServer"
try {
    dotnet run
}
finally {
    Pop-Location
    Write-Host ""
    Write-Host "应用已停止" -ForegroundColor Yellow
}

