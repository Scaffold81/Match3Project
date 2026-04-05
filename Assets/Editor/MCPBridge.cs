// MCPBridge.cs — Unity Editor MCP Bridge
// Установка: скопируй этот файл в Assets/Editor/ любого Unity проекта
// Запускается автоматически при открытии проекта в Unity Editor
// HTTP сервер: http://localhost:7777/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MCPBridge
{
    private const string PREFIX = "http://localhost:7777/";
    private const int MAX_LOGS = 100;

    // Thread-safe log storage
    private static readonly ConcurrentQueue<LogEntry> _logs = new ConcurrentQueue<LogEntry>();

    // Compile errors (cleared on each compile)
    private static readonly List<string> _compileErrors = new List<string>();
    private static readonly object _errorsLock = new object();
    private static volatile bool _isCompiling;

    // Main thread dispatcher
    private static readonly ConcurrentQueue<MainThreadTask> _tasks = new ConcurrentQueue<MainThreadTask>();

    private static HttpListener _listener;
    private static Thread _thread;

    private class MainThreadTask
    {
        public Func<string> Action;
        public string Result;
        public bool Done;
        public readonly object Lock = new object();
    }

    [Serializable]
    private struct LogEntry
    {
        public string msg;
        public string type;
        public string time;
    }

    static MCPBridge()
    {
        Application.logMessageReceived += OnLog;
        CompilationPipeline.compilationStarted += _ =>
        {
            _isCompiling = true;
            lock (_errorsLock) _compileErrors.Clear();
        };
        CompilationPipeline.assemblyCompilationFinished += (path, messages) =>
        {
            lock (_errorsLock)
            {
                foreach (var m in messages)
                    if (m.type == CompilerMessageType.Error)
                        _compileErrors.Add($"{m.file}({m.line},{m.column}): {m.message}");
            }
        };
        CompilationPipeline.compilationFinished += _ => _isCompiling = false;
        EditorApplication.update += OnUpdate;
        EditorApplication.quitting += Stop;
        Start();
    }

    private static void OnLog(string msg, string stack, LogType type)
    {
        while (_logs.Count >= MAX_LOGS) _logs.TryDequeue(out _);
        _logs.Enqueue(new LogEntry
        {
            msg = msg,
            type = type.ToString().ToLower(),
            time = DateTime.Now.ToString("HH:mm:ss"),
        });
    }

    private static void OnUpdate()
    {
        while (_tasks.TryDequeue(out var task))
        {
            task.Result = task.Action();
            lock (task.Lock)
            {
                task.Done = true;
                Monitor.PulseAll(task.Lock);
            }
        }
    }

    private static string RunOnMainThread(Func<string> action)
    {
        var task = new MainThreadTask { Action = action };
        _tasks.Enqueue(task);
        lock (task.Lock)
        {
            while (!task.Done)
                Monitor.Wait(task.Lock, 3000);
        }
        return task.Result ?? "{}";
    }

    private static void Start()
    {
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(PREFIX);
            _listener.Start();
            _thread = new Thread(Listen) { IsBackground = true, Name = "MCPBridge" };
            _thread.Start();
            Debug.Log($"[MCPBridge] Started on {PREFIX}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MCPBridge] Failed to start: {e.Message}");
        }
    }

    private static void Stop()
    {
        _listener?.Stop();
        _thread?.Abort();
    }

    private static void Listen()
    {
        while (_listener.IsListening)
        {
            try
            {
                var ctx = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
            }
            catch { break; }
        }
    }

    private static void Handle(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url.AbsolutePath.TrimEnd('/');
        string json;

        try
        {
            json = path switch
            {
                "/status" => GetStatus(),
                "/logs" => GetLogs(ctx.Request),
                "/errors" => GetErrors(),
                "/scene" => RunOnMainThread(GetScene),
                "/compile" => RunOnMainThread(TriggerCompile),
                _ => "{\"error\":\"Unknown endpoint\"}",
            };
        }
        catch (Exception e)
        {
            json = $"{{\"error\":\"{Escape(e.Message)}\"}}";
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentType = "application/json";
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.Close();
    }

    private static string GetStatus()
    {
        int errCount;
        lock (_errorsLock) errCount = _compileErrors.Count;
        return $"{{\"isCompiling\":{_isCompiling.ToString().ToLower()},\"errorCount\":{errCount}," +
               $"\"projectPath\":\"{Escape(Application.dataPath.Replace("/Assets", ""))}\"," +
               $"\"unityVersion\":\"{Application.unityVersion}\"}}";
    }

    private static string GetLogs(HttpListenerRequest req)
    {
        var typeFilter = req.QueryString["type"];
        var limitStr = req.QueryString["limit"];
        int limit = int.TryParse(limitStr, out var l) ? l : 30;

        var entries = new List<LogEntry>(_logs);
        if (!string.IsNullOrEmpty(typeFilter))
            entries = entries.FindAll(e => e.type == typeFilter);

        var start = Math.Max(0, entries.Count - limit);
        entries = entries.GetRange(start, entries.Count - start);

        var sb = new StringBuilder("[");
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var e = entries[i];
            sb.Append($"{{\"msg\":\"{Escape(e.msg)}\",\"type\":\"{e.type}\",\"time\":\"{e.time}\"}}");
        }
        sb.Append(']');
        return $"{{\"logs\":{sb},\"count\":{entries.Count}}}";
    }

    private static string GetErrors()
    {
        lock (_errorsLock)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < _compileErrors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"{Escape(_compileErrors[i])}\"");
            }
            sb.Append(']');
            return $"{{\"errors\":{sb},\"count\":{_compileErrors.Count}}}";
        }
    }

    private static string GetScene()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var sb = new StringBuilder("[");
        for (int i = 0; i < Math.Min(roots.Length, 50); i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($"\"{Escape(roots[i].name)}\"");
        }
        sb.Append(']');
        return $"{{\"name\":\"{Escape(scene.name)}\",\"path\":\"{Escape(scene.path)}\"," +
               $"\"objectCount\":{roots.Length},\"objects\":{sb}}}";
    }

    private static string TriggerCompile()
    {
        AssetDatabase.Refresh();
        return "{\"status\":\"Compilation triggered\"}";
    }

    private static string Escape(string s) =>
        s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") ?? "";
}
