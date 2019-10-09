# N_m3u8DL-CLI_Core
N_m3u8DL-CLI 的`.NET Core`跨平台实现.  
发布页 https://github.com/nilaoda/N_m3u8DL-CLI_Core/releases

镜像项目 https://github.com/nilaoda/N_m3u8DL-CLI

# 关于开源
本项目已与2019年10月9日开源，代码风格和管理真的很差劲，各取所需就好。

# 如何使用

## 直接运行
移步 [Release](https://github.com/nilaoda/N_m3u8DL-CLI_Core/releases) 页面。

## 自己编译

运行
```
dotnet run
```
编译 Windows
```
dotnet publish -c Release -r win-x64
```
编译 Linux
```
dotnet publish -c Release -r linux-x64
```
编译 MacOS
```
dotnet publish -c Release -r osx-x64
```

# 其他
如果不想让程序生成 `Native Code` ，编辑 `N_m3u8DL-CLI_Core.csproj` 文件，注释掉第`10`行即可。