using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TRnK.Logger;
using UnityEngine;

namespace TRnK.Extensions
{
    internal class YieldTask : CustomYieldInstruction
    {
        public Task Task { get; }
        public YieldTask(Task task) { Task = task; }
        public override bool keepWaiting => !Task.IsCompleted;
    }

    public static class TaskExtensions
    {
        private static SynchronizationContext s_mainContext;
        private static int s_mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureMainThread()
        {
            s_mainContext = SynchronizationContext.Current;
            s_mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>Runs a Task without awaiting it. Exceptions go to onException on the main thread, or to Log.Error; cancellation is ignored.</summary>
        public static async void Forget(this Task task, Action<Exception> onException = null)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                // Called from a worker thread there is no Unity context to resume on, so hop to the main thread for the handler
                if (s_mainContext != null && Thread.CurrentThread.ManagedThreadId != s_mainThreadId)
                    s_mainContext.Post(_ => HandleException(exception, onException), null);
                else
                    HandleException(exception, onException);
            }
        }

        /// <summary>Handles exceptions by invoking the provided callback or logging them.</summary>
        private static void HandleException(Exception ex, Action<Exception> onException)
        {
            // Skip logging OperationCanceledException
            if (ex is OperationCanceledException)
                return;

            if (onException != null)
            {
                onException(ex);
            }
            else
            {
                Log.Error($"Unhandled task exception: {ex}");
            }
        }

        /// <summary>Converts a Task into a coroutine-yieldable IEnumerator.</summary>
        public static IEnumerator AsCoroutine(this Task task)
        {
            yield return new YieldTask(task);

            if (task.IsFaulted)
            {
                throw task.Exception?.GetBaseException() ?? new Exception("Task faulted with no exception.");
            }
            else if (task.IsCanceled)
            {
                throw new OperationCanceledException("Task was canceled.");
            }
        }
    }
}
