using System;
using System.Threading.Tasks;
using UnityEngine;

public class PopupBase<TResult> : MonoBehaviour
{
    // 异步等待核心
    private TaskCompletionSource<TResult> _taskCompletionSource;


    public virtual Task<TResult> ShowAsync()
    {
        gameObject.SetActive(true);
        
        // 创建等待任务
        _taskCompletionSource = new TaskCompletionSource<TResult>();
        return _taskCompletionSource.Task;
    }

    protected void Close(TResult result)
    {
        gameObject.SetActive(false);
        
        // 通知外部：等待结束，返回结果
        _taskCompletionSource?.SetResult(result);
        _taskCompletionSource = null;
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
        _taskCompletionSource = null;
    }
}