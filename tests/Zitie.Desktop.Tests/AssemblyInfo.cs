using Xunit.Sdk;
using Xunit.v3;

// Avalonia Headless 的共享 Application 是进程级的，测试类并行初始化会触发
// "calling thread cannot access this object" 竞态，整个程序集串行执行。
[assembly: ParallelizationAttribute(MaxThreads = 1, Mode = ParallelMode.None)]
