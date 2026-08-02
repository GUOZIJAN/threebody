using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轻量级服务定位器。
/// 各 Manager 在 Awake 中调用 Services.Register(this) 注册自身，
/// 其他类通过 Services.Get&lt;T&gt;() 获取，替代散落的 XXManager.Instance。
///
/// 原有 .Instance 仍然可用，不强制一次性迁移。
/// </summary>
public static class Services
{
    private static readonly Dictionary<Type, object> _services = new();

    /// <summary>注册一个服务实例。</summary>
    public static void Register<T>(T instance) where T : class
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"Services: {type.Name} 已注册，将被覆盖。");
        }
        _services[type] = instance;
    }

    /// <summary>获取已注册的服务实例。未注册时返回 null 并报错。</summary>
    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (!_services.TryGetValue(type, out var instance))
        {
            Debug.LogError($"Services: {type.Name} 未注册！请确保对应的 Manager 已调用 Services.Register。");
            return null;
        }
        return instance as T;
    }

    /// <summary>尝试获取服务，不报错。用于可选依赖。</summary>
    public static bool TryGet<T>(out T result) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var instance))
        {
            result = instance as T;
            return result != null;
        }
        result = null;
        return false;
    }

    /// <summary>注销一个服务。</summary>
    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    /// <summary>清空所有注册的服务（场景切换时调用）。</summary>
    public static void Clear()
    {
        _services.Clear();
    }
}
