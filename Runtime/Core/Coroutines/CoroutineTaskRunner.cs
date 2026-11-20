using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TRnK.Logger;
using UnityEngine;

namespace TRnK.Coroutines
{
    /// <summary>Runs the coroutines behind AsTask and completes every Task, even when its owner is destroyed or deactivated.</summary>
    internal sealed class CoroutineTaskRunner : MonoBehaviour
    {
        private const string RunnerName = "[TRnK] Coroutine Tasks";

        private static CoroutineTaskRunner s_instance;
        private static readonly List<Job> s_jobs = new();
        private static readonly List<Job> s_lost = new();

        private sealed class Job
        {
            public MonoBehaviour Owner;
            public readonly TaskCompletionSource<bool> Source = new();
            public Coroutine Wrapper;
            public CancellationTokenRegistration OwnerDestroyed;
            public CancellationTokenRegistration Canceled;

            // Unity stops an owner's coroutines when its GameObject is destroyed or inactive, but not when the component is disabled
            public bool OwnerLost => Owner == null || !Owner.gameObject.activeInHierarchy;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_instance = null;
            s_jobs.Clear();
            s_lost.Clear();
        }

        /// <summary>Runs the routine; nested IEnumerators run inline so their exceptions reach the Task.</summary>
        internal static Task Run(MonoBehaviour owner, IEnumerator routine, CancellationToken cancellationToken)
        {
            var job = new Job { Owner = owner };
            if (cancellationToken.IsCancellationRequested) return Canceled(job);
            if (job.OwnerLost)
            {
                Log.Warn($"AsTask: '{owner.name}' is inactive, so its coroutine can't run. The Task is canceled.", owner);
                return Canceled(job);
            }

            s_jobs.Add(job);
            job.OwnerDestroyed = owner.destroyCancellationToken.Register(() => Cancel(job));
            if (cancellationToken.CanBeCanceled) job.Canceled = cancellationToken.Register(() => Cancel(job));
            job.Wrapper = Instance.StartCoroutine(StepRoutine(job, routine));
            return job.Source.Task;
        }

        private static CoroutineTaskRunner Instance
        {
            get
            {
                if (s_instance != null) return s_instance;

                var runner = new GameObject(RunnerName);
                if (Application.isPlaying) DontDestroyOnLoad(runner);
                s_instance = runner.AddComponent<CoroutineTaskRunner>();
                return s_instance;
            }
        }

        private static Task Canceled(Job job)
        {
            job.Source.TrySetCanceled();
            return job.Source.Task;
        }

        // Stepping is synchronous, so nothing after a yield runs once the owner is gone
        private static IEnumerator StepRoutine(Job job, IEnumerator routine)
        {
            var chain = new Stack<IEnumerator>();
            chain.Push(routine);

            while (chain.Count > 0)
            {
                if (job.Source.Task.IsCompleted) yield break;
                if (job.OwnerLost)
                {
                    Complete(job, null, canceled: true);
                    yield break;
                }

                IEnumerator current = chain.Peek();
                bool moved;
                try
                {
                    moved = current.MoveNext();
                }
                catch (Exception exception)
                {
                    Complete(job, exception, canceled: false);
                    yield break;
                }

                if (!moved)
                {
                    chain.Pop();
                    continue;
                }

                // Nested routines and CustomYieldInstructions are IEnumerators too: step them here instead of handing them to Unity
                if (current.Current is IEnumerator nested)
                {
                    chain.Push(nested);
                    continue;
                }

                yield return current.Current;
            }

            Complete(job, null, canceled: false);
        }

        private static void Cancel(Job job)
        {
            if (job.Wrapper != null && s_instance != null) s_instance.StopCoroutine(job.Wrapper);
            Complete(job, null, canceled: true);
        }

        private static void Complete(Job job, Exception exception, bool canceled)
        {
            if (!s_jobs.Remove(job)) return;

            job.OwnerDestroyed.Dispose();
            job.Canceled.Dispose();
            if (canceled) job.Source.TrySetCanceled();
            else if (exception != null) job.Source.TrySetException(exception);
            else job.Source.TrySetResult(true);
        }

        // Catches owners that go inactive while a routine waits on a long yield such as WaitForSeconds
        private void Update()
        {
            if (s_jobs.Count == 0) return;

            foreach (Job job in s_jobs)
                if (job.OwnerLost) s_lost.Add(job);

            foreach (Job job in s_lost) Cancel(job);
            s_lost.Clear();
        }

        // Someone destroyed the runner (play mode exit, a full reset): nothing it ran can finish now
        private void OnDestroy()
        {
            if (s_instance != this) return;
            s_instance = null;

            s_lost.AddRange(s_jobs);
            foreach (Job job in s_lost) Complete(job, null, canceled: true);
            s_lost.Clear();
        }
    }
}
