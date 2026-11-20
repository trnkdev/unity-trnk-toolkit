using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TRnK.Coroutines;
using TRnK.Utilities;
using UnityEngine;

namespace TRnK.Extensions
{
    public static class CoroutineExtensions
    {
        /// <summary>Starts multiple coroutines in sequence, one after another.</summary>
        public static Coroutine StartCoroutineSequence(this MonoBehaviour mono, params IEnumerator[] coroutines)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (coroutines.IsNullOrEmpty()) throw new ArgumentException("No coroutines provided.", nameof(coroutines));
            return mono.StartCoroutine(ExecuteSequence(coroutines));
        }

        /// <summary>Starts a coroutine after a specified delay.</summary>
        public static Coroutine StartCoroutineDelayed(this MonoBehaviour mono, IEnumerator coroutine, float delay)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (coroutine == null) throw new ArgumentNullException(nameof(coroutine));
            return mono.StartCoroutine(DelayedExecution(coroutine, delay));
        }

        /// <summary>Starts a coroutine when a condition becomes true.</summary>
        public static Coroutine StartCoroutineWhen(this MonoBehaviour mono, IEnumerator coroutine, Func<bool> condition)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (coroutine == null) throw new ArgumentNullException(nameof(coroutine));
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return mono.StartCoroutine(ConditionalExecution(coroutine, condition));
        }

        /// <summary>Starts multiple coroutines in parallel.</summary>
        public static Coroutine StartCoroutineParallel(this MonoBehaviour mono, params IEnumerator[] coroutines)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (coroutines.IsNullOrEmpty()) throw new ArgumentException("No coroutines provided.", nameof(coroutines));
            return mono.StartCoroutine(ExecuteParallel(mono, coroutines));
        }

        /// <summary>Executes a sequence of coroutines one after another.</summary>
        private static IEnumerator ExecuteSequence(IEnumerator[] coroutines)
        {
            foreach (var coroutine in coroutines)
            {
                yield return coroutine;
            }
        }

        /// <summary>Executes a sequence of coroutines in parallel.</summary>
        private static IEnumerator ExecuteParallel(MonoBehaviour mono, IEnumerator[] coroutines)
        {
            var runningCoroutines = new Coroutine[coroutines.Length];

            for (int i = 0; i < coroutines.Length; i++)
            {
                runningCoroutines[i] = mono.StartCoroutine(coroutines[i]);
            }

            foreach (var coroutine in runningCoroutines)
            {
                yield return coroutine;
            }
        }

        /// <summary>Executes a coroutine after a specified delay.</summary>
        private static IEnumerator DelayedExecution(IEnumerator coroutine, float delay)
        {
            yield return Utils.GetWaitForSeconds(delay);
            yield return coroutine;
        }

        /// <summary>Executes a coroutine when a specified condition is met.</summary>
        private static IEnumerator ConditionalExecution(IEnumerator coroutine, Func<bool> condition)
        {
            yield return new WaitUntil(condition);
            yield return coroutine;
        }

        /// <summary>Runs an IEnumerator as a Task: faulted by an exception anywhere in it, canceled if the owner is destroyed
        /// or deactivated, or by the token. It runs apart from the owner, so the owner's StopAllCoroutines doesn't stop it.</summary>
        public static Task AsTask(this IEnumerator coroutine, MonoBehaviour mono, CancellationToken cancellationToken = default)
        {
            if (coroutine == null) return Task.CompletedTask;
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            return CoroutineTaskRunner.Run(mono, coroutine, cancellationToken);
        }

        /// <summary>Runs multiple coroutines concurrently.</summary>
        public static Task WhenAll(this MonoBehaviour mono, params IEnumerator[] routines)
        {
            if (routines.IsNullOrEmpty())
                return Task.CompletedTask;

            var tasks = new Task[routines.Length];
            for (int i = 0; i < routines.Length; i++)
            {
                tasks[i] = routines[i].AsTask(mono);
            }
            return Task.WhenAll(tasks);
        }

        /// <summary>Runs multiple coroutines and returns when any completes.</summary>
        public static Task<Task> WhenAny(this MonoBehaviour mono, params IEnumerator[] routines)
        {
            if (routines.IsNullOrEmpty())
                return Task.FromResult(Task.CompletedTask);

            var tasks = new Task[routines.Length];
            for (int i = 0; i < routines.Length; i++)
            {
                tasks[i] = routines[i].AsTask(mono);
            }
            return Task.WhenAny(tasks);
        }
    }
}
