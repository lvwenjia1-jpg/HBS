using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TestProject1
{
    /// <summary>
    /// 与 C++ 桥接库一致的枚举（用于转接层，不直接引用 AlibabaCloudRum.dll）
    /// </summary>
    public enum RumLogLevel { Fatal = 1, Error = 2, Warning = 3, Info = 4, Debug = 5, Trace = 6, All = 7 }
    public enum RumEnv { Prod = 1, Gray = 2, Pre = 3, Daily = 4, Local = 5 }

    /// <summary>
    /// C# 转接层：设置 DLL 路径后，按当前进程位数 (x86/x64) 从该路径下的 x86 或 x64 子目录加载桥接库与 native。
    /// </summary>
    public static class RumBridge
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPath);

        private static string _basePath;
        private static Assembly _bridgeAssembly;
        private static object _instance;
        private static Type _rumType;
        private static readonly object _lock = new object();

        /// <summary>
        /// 设置 native/桥接 DLL 所在根路径。实际加载时会使用 path\x86 或 path\x64（根据当前进程位数）。
        /// 必须在首次使用 Instance 之前调用。
        /// </summary>
        public static void SetNativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            lock (_lock)
            {
                if (_instance != null)
                    throw new InvalidOperationException("SetNativePath 必须在首次使用 Instance 之前调用。");
                _basePath = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
        }

        /// <summary>
        /// 获取当前进程应使用的子目录名（x86 或 x64）。
        /// </summary>
        public static string GetCurrentBitnessFolder()
        {
            return IntPtr.Size == 8 ? "x64" : "x86";
        }

        /// <summary>
        /// 当前位数对应的完整 DLL 目录。若 BasePath 已以 \x86 或 \x64 结尾则直接使用，否则拼接 \x86 或 \x64。
        /// </summary>
        private static string GetBridgeDirectory()
        {
            if (string.IsNullOrEmpty(_basePath))
                throw new InvalidOperationException("请先调用 RumBridge.SetNativePath(路径)。");
            string sub = GetCurrentBitnessFolder();
            string dir = _basePath;
            if (!dir.EndsWith("\\x86", StringComparison.OrdinalIgnoreCase) && !dir.EndsWith("\\x64", StringComparison.OrdinalIgnoreCase))
                dir = Path.Combine(_basePath, sub);
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException("未找到桥接库目录: " + dir + "（请先编译并复制 x86/x64 两套输出到该路径）。");
            return dir;
        }

        private static void EnsureLoaded()
        {
            if (_instance != null) return;
            lock (_lock)
            {
                if (_instance != null) return;
                string dir = GetBridgeDirectory();
                if (!SetDllDirectory(dir))
                    throw new InvalidOperationException("SetDllDirectory 失败: " + dir);
                string dllPath = Path.Combine(dir, "AlibabaCloudRum.dll");
                if (!File.Exists(dllPath))
                    throw new FileNotFoundException("未找到桥接库: " + dllPath);
                _bridgeAssembly = Assembly.LoadFrom(dllPath);
                _rumType = _bridgeAssembly.GetType("Alibaba.Cloud.AlibabaCloudRum", true);
                PropertyInfo instanceProp = _rumType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null)
                    throw new MissingMemberException("Alibaba.Cloud.AlibabaCloudRum 未找到静态属性 Instance");
                _instance = instanceProp.GetValue(null);
                if (_instance == null)
                    throw new InvalidOperationException("AlibabaCloudRum.Instance 为 null。");
            }
        }

        /// <summary>
        /// 获取桥接库单例（首次访问时按 SetNativePath 的路径加载对应位数的 DLL）。
        /// </summary>
        public static IRumBridgeInstance Instance
        {
            get
            {
                EnsureLoaded();
                return new RumBridgeInstanceWrapper(_instance, _rumType);
            }
        }
    }

    /// <summary>
    /// 桥接实例的 C# 转接接口，用法与 Alibaba.Cloud.AlibabaCloudRum 一致。
    /// </summary>
    public interface IRumBridgeInstance
    {
        bool Initialize(string appId, string appName, RumEnv env, string configAddress, string appVersion,
            string cachePath, string handlerPath, RumLogLevel logLevel, bool autoCurl, bool autoCrash, bool autoCef);
        void StartSession();
        void EndSession();
        void SetUsername(string username);
        void SetUserId(string userId);
        void SetUserTags(string userTagsJson);
        void ReportCustomLog(string type, string name, RumLogLevel level, string content);
        void ReportCustomException(string name, string message, string file, string source, string stack);
        void SetProperties(System.Collections.Generic.Dictionary<string, string> properties);
        void Close();
        void Dispose();
    }

    internal class RumBridgeInstanceWrapper : IRumBridgeInstance
    {
        private readonly object _instance;
        private readonly Type _type;

        internal RumBridgeInstanceWrapper(object instance, Type type)
        {
            _instance = instance;
            _type = type;
        }

        private object Invoke(string methodName, params object[] args)
        {
            MethodInfo m = _type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (m == null) throw new MissingMethodException(_type.FullName, methodName);
            return m.Invoke(_instance, args);
        }

        public bool Initialize(string appId, string appName, RumEnv env, string configAddress, string appVersion,
            string cachePath, string handlerPath, RumLogLevel logLevel, bool autoCurl, bool autoCrash, bool autoCef)
        {
            object r = Invoke("Initialize", appId, appName, (int)env, configAddress, appVersion,
                cachePath, handlerPath, (int)logLevel, autoCurl, autoCrash, autoCef);
            return (bool)r;
        }

        public void StartSession() => Invoke("StartSession");
        public void EndSession() => Invoke("EndSession");
        public void SetUsername(string username) => Invoke("SetUsername", username);
        public void SetUserId(string userId) => Invoke("SetUserId", userId);
        public void SetUserTags(string userTagsJson) => Invoke("SetUserTags", userTagsJson);

        public void ReportCustomLog(string type, string name, RumLogLevel level, string content)
        {
            Invoke("ReportCustomLog", type, name, (int)level, content);
        }

        public void ReportCustomException(string name, string message, string file, string source, string stack)
        {
            Invoke("ReportCustomException", name, message ?? "", file ?? "", source ?? "", stack ?? "");
        }

        public void SetProperties(System.Collections.Generic.Dictionary<string, string> properties)
        {
            Invoke("SetProperties", properties);
        }

        public void Close() => Invoke("Close");

        public void Dispose()
        {
            try { Invoke("Dispose"); } catch { }
        }
    }
}
