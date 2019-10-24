using N_m3u8DL_CLI_Core.CommandLineParser;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace N_m3u8DL_CLI_Core
{
    class Program
    {
        public static bool isWindows = true;

        static void Main(string[] args)
        {
            try
            {
                //判断操作系统
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    isWindows = false;

                string CURRENT_PATH = Directory.GetCurrentDirectory();
                string fileName = "";
                string exePath = AppDomain.CurrentDomain.BaseDirectory;

                //goto HasFFmpeg;
                //寻找ffmpeg.exe
                if (!File.Exists("ffmpeg.exe"))
                {
                    try
                    {
                        string[] EnvironmentPath = null;

                        if (isWindows)
                            EnvironmentPath = Environment.GetEnvironmentVariable("Path").Split(';');
                        else
                            EnvironmentPath = Environment.GetEnvironmentVariable("PATH").Split(':');

                        foreach (var de in EnvironmentPath)
                        {
                            if (File.Exists(Path.Combine(de.Trim('\"').Trim(),
                                "ffmpeg" + (isWindows ? ".exe" : ""))))
                                goto HasFFmpeg;
                        }
                    }
                    catch (Exception)
                    {
                        ;
                    }

                    if (isWindows)
                    {
                        Console.ForegroundColor = ConsoleColor.White; //设置前景色，即字体颜色
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("在PATH和程序路径下找不到 ffmpeg.exe");
                        Console.ResetColor(); //将控制台的前景色和背景色设为默认值
                        Console.WriteLine("请下载ffmpeg.exe并把他放到程序同目录.");
                        Console.WriteLine();
                        Console.WriteLine("x86 https://ffmpeg.zeranoe.com/builds/win32/static/");
                        Console.WriteLine("x64 https://ffmpeg.zeranoe.com/builds/win64/static/");
                        Console.WriteLine();
                        Console.WriteLine("按任意键退出.");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White; //设置前景色，即字体颜色
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("在PATH和程序路径下找不到 ffmpeg");
                        Console.ResetColor(); //将控制台的前景色和背景色设为默认值
                        Console.WriteLine("请自行安装ffmpeg.");
                        Console.WriteLine();
                        Console.WriteLine("按任意键退出.");
                        Environment.Exit(-1);
                    }
                }

            HasFFmpeg:
                Global.WriteInit();
                Thread checkUpdate = new Thread(() =>
                {
                    //Global.CheckUpdate();
                });
                checkUpdate.IsBackground = true;
                checkUpdate.Start();

                int maxThreads = Environment.ProcessorCount;
                int minThreads = 16;
                int retryCount = 15;
                int timeOut = 10; //默认10秒
                string baseUrl = "";
                string reqHeaders = "";
                string keyFile = "";
                string keyBase64 = "";
                string muxSetJson = "MUXSETS.json";
                string workDir = Path.Combine(CURRENT_PATH, "Downloads");
                bool muxFastStart = false;
                bool binaryMerge = false;
                bool delAfterDone = false;
                bool parseOnly = false;
                bool noMerge = false;

                /******************************************************/
                ServicePointManager.DefaultConnectionLimit = 1024;
                /******************************************************/

                if (File.Exists("headers.txt"))
                    reqHeaders = File.ReadAllText("headers.txt");

                //分析命令行参数
                parseArgs:
                var arguments = CommandLineArgumentParser.Parse(args);
                if (args.Length == 1 && args[0] == "--help")
                {
                    Console.WriteLine(StaticText.HELP);
                    return;
                }

                if (arguments.Has("--enableDelAfterDone"))
                {
                    delAfterDone = true;
                }
                if (arguments.Has("--enableParseOnly"))
                {
                    parseOnly = true;
                }
                if (arguments.Has("--enableBinaryMerge"))
                {
                    binaryMerge = true;
                }
                if (arguments.Has("--disableDateInfo"))
                {
                    FFmpeg.WriteDate = false;
                }
                if (arguments.Has("--noMerge"))
                {
                    noMerge = true;
                }
                if (arguments.Has("--noProxy"))
                {
                    Global.NoProxy = true;
                }
                if (arguments.Has("--headers"))
                {
                    reqHeaders = arguments.Get("--headers").Next;
                }
                if (arguments.Has("--enableMuxFastStart"))
                {
                    muxFastStart = true;
                }
                if (arguments.Has("--disableIntegrityCheck"))
                {
                    DownloadManager.DisableIntegrityCheck = true;
                }
                if (arguments.Has("--enableAudioOnly"))
                {
                    Global.VIDEO_TYPE = "IGNORE";
                }
                if (arguments.Has("--muxSetJson"))
                {
                    muxSetJson = arguments.Get("--muxSetJson").Next;
                }
                if (arguments.Has("--workDir"))
                {
                    workDir = arguments.Get("--workDir").Next;
                    DownloadManager.HasSetDir = true;
                }
                if (arguments.Has("--saveName"))
                {
                    fileName = arguments.Get("--saveName").Next;
                }
                if (arguments.Has("--useKeyFile"))
                {
                    if (File.Exists(arguments.Get("--useKeyFile").Next))
                        keyFile = arguments.Get("--useKeyFile").Next;
                }
                if (arguments.Has("--useKeyBase64"))
                {
                    keyBase64 = arguments.Get("--useKeyBase64").Next;
                }
                if (arguments.Has("--stopSpeed"))
                {
                    Global.STOP_SPEED = Convert.ToInt64(arguments.Get("--stopSpeed").Next);
                }
                if (arguments.Has("--maxSpeed"))
                {
                    Global.MAX_SPEED = Convert.ToInt64(arguments.Get("--maxSpeed").Next);
                }
                if (arguments.Has("--baseUrl"))
                {
                    baseUrl = arguments.Get("--baseUrl").Next;
                }
                if (arguments.Has("--maxThreads"))
                {
                    maxThreads = Convert.ToInt32(arguments.Get("--maxThreads").Next);
                }
                if (arguments.Has("--minThreads"))
                {
                    minThreads = Convert.ToInt32(arguments.Get("--minThreads").Next);
                }
                if (arguments.Has("--retryCount"))
                {
                    retryCount = Convert.ToInt32(arguments.Get("--retryCount").Next);
                }
                if (arguments.Has("--timeOut"))
                {
                    timeOut = Convert.ToInt32(arguments.Get("--timeOut").Next);
                }
                if (arguments.Has("--downloadRange"))
                {
                    string p = arguments.Get("--downloadRange").Next;

                    if (p.Contains(":"))
                    {
                        //时间码
                        Regex reg2 = new Regex(@"((\d+):(\d+):(\d+))?-((\d+):(\d+):(\d+))?");
                        if (reg2.IsMatch(p))
                        {
                            Parser.DurStart = reg2.Match(p).Groups[1].Value;
                            Parser.DurEnd = reg2.Match(p).Groups[5].Value;
                            Parser.DelAd = false;
                        }
                    }
                    else
                    {
                        //数字
                        Regex reg = new Regex(@"(\d*)-(\d*)");
                        if (reg.IsMatch(p))
                        {
                            if (!string.IsNullOrEmpty(reg.Match(p).Groups[1].Value))
                            {
                                Parser.RangeStart = Convert.ToInt32(reg.Match(p).Groups[1].Value);
                                Parser.DelAd = false;
                            }
                            if (!string.IsNullOrEmpty(reg.Match(p).Groups[2].Value))
                            {
                                Parser.RangeEnd = Convert.ToInt32(reg.Match(p).Groups[2].Value);
                                Parser.DelAd = false;
                            }
                        }
                    }
                }

                //如果只有URL，没有附加参数，则尝试解析配置文件
                if (args.Length == 1)
                {
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "N_m3u8DL-CLI.args.txt")))
                    {
                        args = Global.ParseArguments($"\"{args[0]}\"" + File.ReadAllText(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "N_m3u8DL-CLI.args.txt"))).ToArray();  //解析命令行
                        goto parseArgs;
                    }
                }

                //ReadLine字数上限
                Stream steam = Console.OpenStandardInput();
                Console.SetIn(new StreamReader(steam, Encoding.Default, false, 5000));
                int inputRetryCount = 20;
            input:
                string testurl = "";


                //重试太多次，退出
                if (inputRetryCount == 0)
                    Environment.Exit(-1);

                if (args.Length > 0)
                    testurl = args[0];
                else
                {
                    Console.CursorVisible = true;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("N_m3u8DL-CLI");
                    Console.ResetColor();
                    Console.Write(" > ");

                    if (isWindows)
                        args = Global.ParseArguments(Console.ReadLine()).ToArray();  //解析命令行
                    else
                        args = Global.SplitCommandLine(Console.ReadLine());  //解析命令行
                    Global.WriteInit();
                    Console.CursorVisible = false;
                    goto parseArgs;
                }

                if (fileName == "")
                    fileName = Global.GetUrlFileName(testurl) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");


                //优酷DRM设备更改
                /*if (testurl.Contains("playlist/m3u8"))
                {
                    string drm_type = Global.GetQueryString("drm_type", testurl);
                    string drm_device = Global.GetQueryString("drm_device", testurl);
                    if (drm_type != "1")
                    {
                        testurl = testurl.Replace("drm_type=" + drm_type, "drm_type=1");
                    }
                    if (drm_device != "11")
                    {
                        testurl = testurl.Replace("drm_device=" + drm_device, "drm_device=11");
                    }
                }*/
                string m3u8Content = string.Empty;
                bool isVOD = true;



                //开始解析

                Console.CursorVisible = false;
                LOGGER.PrintLine($"文件名称：{fileName}");
                LOGGER.PrintLine($"存储路径：{Path.GetDirectoryName(Path.Combine(workDir, fileName))}");

                Parser parser = new Parser();
                parser.DownName = fileName;
                parser.DownDir = Path.Combine(workDir, parser.DownName);
                parser.M3u8Url = testurl;
                parser.KeyBase64 = keyBase64;
                parser.KeyFile = keyFile;
                if (baseUrl != "")
                    parser.BaseUrl = baseUrl;
                parser.Headers = reqHeaders;
                LOGGER.LOGFILE = Path.Combine(exePath, "Logs", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log");
                LOGGER.InitLog();
                LOGGER.WriteLine("Start Parsing " + testurl);
                LOGGER.PrintLine("开始解析地址...", LOGGER.Warning);
                if (testurl.EndsWith(".json") && File.Exists(testurl))  //可直接跳过解析
                {
                    if (!Directory.Exists(Path.Combine(workDir, fileName)))//若文件夹不存在则新建文件夹   
                        Directory.CreateDirectory(Path.Combine(workDir, fileName)); //新建文件夹  
                    File.Copy(testurl, Path.Combine(Path.Combine(workDir, fileName), "meta.json"));
                }
                else
                {
                    parser.Parse();  //开始解析
                }

                //仅解析模式
                if (parseOnly)
                {
                    LOGGER.PrintLine("解析m3u8成功, 程序退出");
                    Environment.Exit(0);
                }

                if (File.Exists(Path.Combine(Path.Combine(workDir, fileName), "meta.json")))
                {
                    JObject initJson = JObject.Parse(File.ReadAllText(Path.Combine(Path.Combine(workDir, fileName), "meta.json")));
                    isVOD = Convert.ToBoolean(initJson["m3u8Info"]["vod"].ToString());
                    //传给Watcher总时长
                    Watcher.TotalDuration = initJson["m3u8Info"]["totalDuration"].Value<double>();
                    LOGGER.PrintLine($"文件时长：{Global.FormatTime((int)Watcher.TotalDuration)}");
                    LOGGER.PrintLine("总分片：" + initJson["m3u8Info"]["originalCount"].Value<int>()
                        + ", 已选择分片：" + initJson["m3u8Info"]["count"].Value<int>());
                }
                else
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(workDir, fileName));
                    directoryInfo.Delete(true);
                    LOGGER.PrintLine("地址无效", LOGGER.Error);
                    LOGGER.CursorIndex = 5;
                    inputRetryCount--;
                    goto input;
                }

                //点播
                if (isVOD == true)
                {
                    ServicePointManager.DefaultConnectionLimit = 10000;
                    DownloadManager md = new DownloadManager();
                    md.DownDir = parser.DownDir;
                    md.Headers = reqHeaders;
                    md.Threads = Environment.ProcessorCount;
                    if (md.Threads > maxThreads)
                        md.Threads = maxThreads;
                    if (md.Threads < minThreads)
                        md.Threads = minThreads;
                    if (File.Exists("minT.txt"))
                    {
                        int t = Convert.ToInt32(File.ReadAllText("minT.txt"));
                        if (md.Threads <= t)
                            md.Threads = t;
                    }
                    md.TimeOut = timeOut * 1000;
                    md.NoMerge = noMerge;
                    md.DownName = fileName;
                    md.DelAfterDone = delAfterDone;
                    md.BinaryMerge = binaryMerge;
                    md.MuxFormat = "mp4";
                    md.RetryCount = retryCount;
                    md.MuxSetJson = muxSetJson;
                    md.MuxFastStart = muxFastStart;
                    md.DoDownload();
                }
                //直播
                if (isVOD == false)
                {
                    LOGGER.WriteLine("Living Stream Found");
                    LOGGER.WriteLine("Start Recording");
                    LOGGER.PrintLine("识别为直播流, 开始录制");
                    LOGGER.STOPLOG = true;  //停止记录日志
                                            //开辟文件流，且不关闭。（便于播放器不断读取文件）
                    string LivePath = Path.Combine(Directory.GetParent(parser.DownDir).FullName
                        , DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + fileName + ".ts");
                    FileStream outputStream = new FileStream(LivePath, FileMode.Append);

                    HLSLiveDownloader live = new HLSLiveDownloader();
                    live.DownDir = parser.DownDir;
                    live.Headers = reqHeaders;
                    live.LiveStream = outputStream;
                    live.LiveFile = LivePath;
                    live.TimerStart();  //开始录制
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
