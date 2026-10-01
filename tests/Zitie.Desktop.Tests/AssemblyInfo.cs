using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Xunit.Sdk;
using Xunit.v3;
using Zitie.Desktop.Tests;

// 官方 Headless 测试模式：App 入口 + Avalonia 测试框架（UI 线程由框架管理）。
// 仍保持串行，避免与个别非 UI 测试的时序干扰。
[assembly: AvaloniaTestApplication(typeof(ThemeTestAppBuilder))]
[assembly: AvaloniaTestFramework]
[assembly: Parallelization(MaxThreads = 1, Mode = ParallelMode.None)]
