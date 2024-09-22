using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.Util
{
    /// <summary>
    /// 日志工具
    /// </summary>
    public class LogUtil
    {
        /// <summary>
        /// 日志前缀
        /// </summary>
        private readonly string _logPrefix;

        /// <summary>
        /// 日志路径
        /// </summary>
        private string _logPath;

        /// <summary>
        /// 日志队列
        /// </summary>
        private readonly ConcurrentQueue<string> _logQueue = new();

        /// <summary>
        /// 存活状态
        /// </summary>
        private readonly bool _isAlive = false;

        /// <summary>
        /// 线程休眠间隔
        /// </summary>
        private readonly double _threadSleepInterval = 1;

        /// <summary>
        /// 日志处理
        /// </summary>
        private readonly Action<string, StringBuilder> _logHandler;

        /// <summary>
        /// 日志等级
        /// </summary>
        private byte _logLevel = 1;

        /// <summary>
        /// 当前文件名
        /// </summary>
        private string _currentFileName;

        /// <summary>
        /// 下标
        /// </summary>
        private int _index;

        /// <summary>
        /// 上次刷新文件名时间
        /// </summary>
        private DateTime _lastRefreshFileNameTime = DateTime.MinValue;

        /// <summary>
        /// 日志名称字典
        /// </summary>
        private readonly ConcurrentDictionary<DateTime, ConcurrentDictionary<int, string>> _logNameDic = new();

        /// <summary>
        /// 
        /// </summary>
        private bool _isWrite = false;

        /// <summary>
        /// 是否写日志
        /// </summary>
        private bool IsWrite
        {
            set
            {
                if (string.IsNullOrWhiteSpace(_logPath))
                {
                    if (_isWrite)
                        _isWrite = false;
                    return;
                }

                _isWrite = value;
            }
            get => _isWrite;
        }

        /// <summary>
        /// 保存天数
        /// </summary>
        private int _saveDay = 3;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogUtil()
        {
            _isAlive = true;
            (new Thread(OnWork) { IsBackground = true }).Start();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        public LogUtil(string logPrefix) : this()
        {
            _logPrefix = logPrefix;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        public LogUtil(string logPrefix, string logPath) : this(logPrefix)
        {
            _logPath = logPath;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        /// <param name="threadSleepInterval"></param>
        public LogUtil(string logPrefix, string logPath, double threadSleepInterval) : this(logPrefix, logPath)
        {
            _threadSleepInterval = threadSleepInterval;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        /// <param name="threadSleepInterval"></param>
        /// <param name="logHandler"></param>
        public LogUtil(string logPrefix, string logPath, double threadSleepInterval, Action<string, StringBuilder> logHandler) : this(logPrefix, logPath, threadSleepInterval)
        {
            _logHandler = logHandler;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        /// <param name="threadSleepInterval"></param>
        /// <param name="logHandler"></param>
        /// <param name="logLevel">日志等级</param>
        public LogUtil(string logPrefix, string logPath, double threadSleepInterval, Action<string, StringBuilder> logHandler,
            byte logLevel = 3) : this(logPrefix, logPath, threadSleepInterval, logHandler)
        {
            this._logLevel = logLevel;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        /// <param name="threadSleepInterval"></param>
        /// <param name="logHandler"></param>
        /// <param name="isWrite"></param>
        public LogUtil(string logPrefix, string logPath, double threadSleepInterval, Action<string, StringBuilder> logHandler,
            bool isWrite) : this(logPrefix, logPath, threadSleepInterval, logHandler)
        {
            this.IsWrite = isWrite;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        /// <param name="threadSleepInterval"></param>
        /// <param name="logHandler"></param>
        /// <param name="isWrite"></param>
        /// <param name="logLevel">日志等级</param>
        public LogUtil(string logPrefix, string logPath, double threadSleepInterval, Action<string, StringBuilder> logHandler,
            bool isWrite, byte logLevel = 3) : this(logPrefix, logPath, threadSleepInterval, logHandler, isWrite)
        {
            _logLevel = logLevel;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPrefix"></param>
        /// <param name="logPath"></param>
        /// <param name="threadSleepInterval"></param>
        /// <param name="logHandler"></param>
        /// <param name="isWrite"></param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="saveDay"></param>
        public LogUtil(string logPrefix, string logPath, double threadSleepInterval, Action<string, StringBuilder> logHandler,
            bool isWrite, byte logLevel = 3, int saveDay = 3) : this(logPrefix, logPath, threadSleepInterval, logHandler, isWrite, logLevel)
        {
            _saveDay = saveDay;
        }

        /// <summary>
        /// 线程处理
        /// </summary>
        /// <param name="obj"></param>
        private void OnWork(object? obj)
        {
            while (_isAlive)
            {
                if (IsWrite && DateTime.Now > _lastRefreshFileNameTime)
                {
                    RefreshLogFileName();
                    ValidationRequiresCompression();
                }

                Exec();
                //Thread.Sleep(_threadSleepInterval * 1000);
                Thread.Sleep((int)(_threadSleepInterval * 1000));
            }
        }

        /// <summary>
        /// 校验是否需要压缩
        /// </summary>
        private void ValidationRequiresCompression()
        {
            if (_logNameDic.Count <= 1)
                return;
            var keys = _logNameDic.Keys.Where(t => t != DateTime.Now.Date).ToList();
            keys.ForEach(k =>
            {
                if (!_logNameDic.TryRemove(k, out var logDictionary))
                    return;
                CompressFiles(logDictionary.Values.Select(t => Path.Combine(_logPath, t)).ToArray(), _logPath, $"{_logPrefix}_{k:yyyyMMdd}", "zip", true);
            });
        }

        /// <summary>
        /// 刷新日志文件名
        /// </summary>
        private void RefreshLogFileName()
        {
            VerifyLogFiles();
            try
            {
                ConcurrentDictionary<int, string> logDictionary;
                if (string.IsNullOrWhiteSpace(_currentFileName) || DateTime.Now.Date != _lastRefreshFileNameTime.Date)
                {
                    _index = 0;
                    _lastRefreshFileNameTime = DateTime.Now;
                    _currentFileName = $"{_logPrefix}_{_lastRefreshFileNameTime:yyyyMMdd}_{_index}.log";
                    var isNew = true;
                    if (File.Exists(Path.Combine(_logPath, _currentFileName)))
                    {
                        Init();
                        isNew = false;
                    }

                    logDictionary = new ConcurrentDictionary<int, string>();
                    logDictionary.TryAdd(_index, _currentFileName);
                    if (isNew)
                        _index++;
                    _logNameDic.TryAdd(_lastRefreshFileNameTime.Date, logDictionary);
                    return;
                }
                var fileInfo = new FileInfo(Path.Combine(_logPath, _currentFileName));
                if (!fileInfo.Exists || fileInfo.Length < 10 * 1024 * 1024)
                    return;
                if (_logNameDic.TryGetValue(_lastRefreshFileNameTime.Date, out logDictionary))
                {
                    _currentFileName = $"{_logPrefix}_{_lastRefreshFileNameTime:yyyyMMdd}_{_index++}.log";
                    logDictionary.TryAdd(_index, _currentFileName);
                }
            }
            catch
            {
                // 忽略
            }
            finally
            {
                _lastRefreshFileNameTime = DateTime.Now.AddSeconds(3);
            }
        }

        /// <summary>
        /// 校验日志文件
        /// </summary>
        private void VerifyLogFiles()
        {
            try
            {
                //删文件 压缩流程
                if (_logNameDic.IsEmpty || _logNameDic.Keys.Count == 1)
                    return;
                //压缩上一天的，删除超过保存天数的
                for (var day = 1; day < _saveDay; day++)
                {
                    if (_logNameDic.TryGetValue(DateTime.Now.Date.AddDays(-day), out var logDictionary))
                    {
                        var keys = logDictionary.Keys.ToList();
                        CompressFiles(logDictionary.Select(t => $"{Path.Combine(_logPath, t.Value)}").ToArray(), _logPath, $"{_logPrefix}_{DateTime.Now.Date.AddDays(-day):yyyyMMdd}", "zip", true);
                        _logNameDic.TryRemove(DateTime.Now.Date.AddDays(-day), out _);
                    }
                }
                var delKeys = _logNameDic
                    .Where(t => t.Key != DateTime.Now.Date)
                    .Select(t => t.Key);
                foreach (var key in delKeys)
                {
                    if (!_logNameDic.TryRemove(key, out var logDictionary))
                        continue;
                    logDictionary.Keys.ToList().ForEach(d =>
                    {
                        if (logDictionary.TryRemove(d, out var fileName))
                            File.Delete(Path.Combine(_logPath, fileName));
                    });
                }

            }
            catch (Exception e)
            {
                //
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            if (!Directory.Exists(_logPath))
                return;
            var files = Directory.GetFiles(_logPath, $"{_logPrefix}_*_*.log", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fileNameSplit = Path.GetFileNameWithoutExtension(file).Split('_');
                if (fileNameSplit.Length != 3)
                    continue;
                if (!DateTime.TryParseExact(fileNameSplit[1], "yyyyMMdd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var time)
                    || !int.TryParse(fileNameSplit[2], out var index))
                    continue;
                if (!_logNameDic.TryGetValue(time.Date, out var lConcurrentDictionary))
                {
                    lConcurrentDictionary = new ConcurrentDictionary<int, string>();
                    _logNameDic.TryAdd(time.Date, lConcurrentDictionary);
                }
                lConcurrentDictionary.TryAdd(index, Path.GetFileName(file));
            }

            if (_logNameDic.TryGetValue(DateTime.Now.Date, out var logDictionary))
            {
                if (logDictionary.Count > 0)
                {
                    var index = logDictionary.Keys.Max();
                    var fileInfo = new FileInfo(Path.Combine(_logPath, logDictionary[index]));
                    if (fileInfo.Length < 10 * 1024 * 1024)
                    {
                        _index = index;
                        _currentFileName = logDictionary[index];
                        return;
                    }
                    _index = index + 1;
                    _currentFileName = $"{_logPrefix}_{DateTime.Now.Date}_{_index}.log";
                }
            }
        }

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="files">文件集</param>
        /// <param name="compressionPath">压缩到哪个目录</param>
        /// <param name="compressionName">压缩的文件名称</param>
        /// <param name="type">压缩类型</param>
        /// <param name="isDelete">删除原文件</param>
        private bool CompressFiles(string[] files, string compressionPath, string compressionName, string type = "zip", bool isDelete = false)
        {
            try
            {
                var taskList = new List<Task>();
                if (type.Equals("zip"))
                {
                    using var zipCreate = File.Create(Path.Combine(compressionPath, compressionName + ".zip"));
                    using var archive = new ZipArchive(zipCreate, ZipArchiveMode.Create);
                    foreach (var file in files)
                    {
                        var task = Task.Run(() =>
                        {
                            var zipEntity = archive.CreateEntry(Path.GetFileName(file));
                            using var entityStream = zipEntity.Open();
                            using (var fileStream = File.OpenRead(file))
                            {
                                fileStream.CopyTo(entityStream);
                            }

                            if (isDelete)
                                File.Delete(file);
                        });
                        taskList.Add(task);
                        task.ContinueWith(t =>
                        {
                            t.Dispose();
                            if (taskList != null && taskList.Count > 0)
                                taskList.Remove(t);
                        });
                    }
                    Task.WaitAll(taskList.ToArray());
                    if (taskList.Any())
                        taskList.ForEach(t => t.Dispose());
                    taskList.Clear();
                }
                else
                {
                    var gzFiles = new List<string>();
                    foreach (var file in files)
                    {
                        var task = Task.Run(() =>
                        {
                            var gzFileName = file + ".gz";
                            gzFiles.Add(gzFileName);
                            using (var inputFileStream = File.OpenRead(file))
                            {
                                using (var outputFileStream = File.Create(gzFileName))
                                {
                                    using var compressionStream = new GZipStream(outputFileStream, CompressionMode.Compress);
                                    inputFileStream.CopyTo(compressionStream);
                                }
                            }
                            if (isDelete)
                                File.Delete(file);
                        });
                        taskList.Add(task);
                        task.ContinueWith(t =>
                        {
                            t.Dispose();
                            if (taskList != null && taskList.Count > 0)
                                taskList.Remove(t);
                        });
                    }
                    CompressFiles(gzFiles.ToArray(), compressionPath, compressionName = $"{compressionName}.gz", isDelete: true);
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Exec()
        {
            if (_logQueue.IsEmpty)
                return;
            var count = 1000;
            var sb = new StringBuilder();
            while (_logQueue.TryDequeue(out var str))
            {
                if (!string.IsNullOrWhiteSpace(str))
                    if (!str.EndsWith("\r\n"))
                        sb.AppendLine(str);
                    else
                        sb.Append(str);
                count--;
                if (count <= 0)
                    break;
            }
            if (sb.Length <= 0)
                return;
            //sb.AppendLine();
            try
            {
                _logHandler?.Invoke(_logPrefix, sb);
                if (IsWrite)
                    WriteLog(sb.ToString());
            }
            catch
            {
                // 忽略
            }
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="str"></param>
        private void WriteLog(string str)
        {
            if (string.IsNullOrWhiteSpace(_currentFileName))
                return;
            if (!Directory.Exists(_logPath))
                Directory.CreateDirectory(_logPath);
            using var fs = new FileStream(Path.Combine(_logPath, _currentFileName), FileMode.OpenOrCreate, FileAccess.Write);
            var createContent = Encoding.UTF8.GetBytes(str);
            fs.Seek(fs.Length, SeekOrigin.Current);
            fs.Write(createContent, 0, createContent.Length);
            fs.Flush();
        }

        /// <summary>
        /// 设置日志等级
        /// </summary>
        /// <param name="logLevel"></param>
        public void SetLogLevel(byte logLevel) => _logLevel = logLevel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isWrite"></param>
        public void SetWrite(bool isWrite) => IsWrite = isWrite;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void SetLogPath(string path) => _logPath = path;

        /// <summary>
        /// 设置保存天数
        /// </summary>
        /// <param name="day"></param>
        public void SetLogSaveDay(int day) => _saveDay = day;

        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="str"></param>
        public virtual void Warning(string str)
        {
            if (_logLevel < 2 || string.IsNullOrWhiteSpace(str))
                return;
            _logQueue.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [tid={Thread.CurrentThread.ManagedThreadId}] [Warning] {str}");
        }

        /// <summary>
        /// 调试日志
        /// </summary>
        /// <param name="str"></param>
        public virtual void Debug(string str)
        {
            if (_logLevel < 3 || string.IsNullOrWhiteSpace(str))
                return;
            _logQueue.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [tid={Thread.CurrentThread.ManagedThreadId}] [Debug] {str}");
        }

        /// <summary>
        /// 信息日志
        /// </summary>
        /// <param name="str"></param>
        public virtual void Info(string str)
        {
            if (_logLevel < 1 || string.IsNullOrWhiteSpace(str))
                return;
            _logQueue.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [tid={Thread.CurrentThread.ManagedThreadId}] [Info] {str}");
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ex"></param>
        public virtual void Error(string str, Exception? ex = null)
        {
            if (string.IsNullOrWhiteSpace(str) && ex == null)
                return;
            var exBuild = new StringBuilder();
            if (ex != null)
            {
                exBuild.AppendLine($"StackTrace:{ex.StackTrace}");
                exBuild.AppendLine($"Message:{ex.Message}");
            }
            _logQueue.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [tid={Thread.CurrentThread.ManagedThreadId}] [Error] {str},{exBuild}");
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            while (_logQueue.TryDequeue(out _))
            {

            }
        }
    }
}
