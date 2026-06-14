using System;
using System.Collections;
using Unity.Jobs;
using UnityEngine;


public class MyJobRunner
{
    public bool Running => running_job_handle != null;

    JobHandle? running_job_handle;
    Action callback_when_complete;
    MonoBehaviour coro_mono;
    Coroutine coro;

    public void Start<T>(MonoBehaviour mono, ref T job, Action callback) where T : struct, IJob
    {
        Complete();
        var handle = job.Schedule();
        running_job_handle = handle;
        callback_when_complete = callback;
        coro_mono = mono;
        coro = mono.StartCoroutine(CheckComplete());
    }

    IEnumerator CheckComplete()
    {
        do {
            yield return null;
        } while (!running_job_handle.Value.IsCompleted);

        running_job_handle.Value.Complete();
        coro = null;
        coro_mono = null;
        running_job_handle = null;

        Action act = callback_when_complete;
        callback_when_complete = null;
        act?.Invoke();
    }

    public void Complete()
    {
        if (coro != null && coro_mono != null)
            coro_mono.StopCoroutine(coro);
        coro = null;
        coro_mono = null;

        if (running_job_handle != null)
        {
            running_job_handle.Value.Complete();
            running_job_handle = null;
        }

        Action act = callback_when_complete;
        callback_when_complete = null;
        act?.Invoke();
    }

    public void Dispose()
    {
        callback_when_complete = null;
        Complete();
    }
}
