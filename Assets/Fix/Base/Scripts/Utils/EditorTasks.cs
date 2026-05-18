using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fix.Editor
{
    public class EditorTasks
    {
        private const int DefaultMaxTaskCount = 50;
        private const int ThreadSleepTime = 200;
        private readonly object SyncRoot = new object();
        private readonly List<Task> parallel;
        private readonly Queue<Task> tasks;
        private Action<int, bool> OnProcess;
        private Action OnStart, OnComplete;
        private bool started;

        private int completedCount;

        public bool IsCompleted
        {
            get
            {
                Thread.Sleep(ThreadSleepTime);
                lock (SyncRoot)
                {
                    return completedCount == Count;
                }
            }
        }

        public int CompletedCount
        {
            get
            {
                lock (SyncRoot)
                {
                    return completedCount;
                }
            }
        }

        public int Count { get; }
        public float Progress => (float) CompletedCount / Count;

        public int MaxTaskCount { get; }

        public EditorTasks(IEnumerable<Action> actions,
            Action<int, bool> onProcess,
            Action onStart, Action onComplete,
            int maxTaskCount = DefaultMaxTaskCount)
        {
            tasks = new Queue<Task>();
            foreach (var task in actions.ToArray().Select((e, i) => new Task(() =>
            {
                bool success = true;
                try
                {
                    e();
                }
                catch
                {
                    success = false;
                    throw;
                }
                finally
                {
                    lock (SyncRoot)
                    {
                        ++completedCount;
                        try
                        {
                            OnProcess?.Invoke(completedCount, success);
                        }
                        catch
                        {
                        }

                        if (completedCount == Count)
                        {
                            try
                            {
                                OnComplete?.Invoke();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            })))
            {
                task.ContinueWith(e =>
                {
                    lock (SyncRoot)
                    {
                        parallel.Remove(e);
                        while (tasks.Count != 0 && parallel.Count < MaxTaskCount)
                            parallel.Add(StartTask(tasks.Dequeue()));
                    }
                });
                tasks.Enqueue(task);
            }

            Count = tasks.Count;
            MaxTaskCount = maxTaskCount;
            OnProcess = onProcess;
            OnStart = onStart;
            OnComplete = onComplete;
            parallel = new List<Task>();
            while (tasks.Count != 0 && parallel.Count < MaxTaskCount)
                parallel.Add(tasks.Dequeue());
        }

        public EditorTasks(IEnumerable<Action> actions, int maxTaskCount = DefaultMaxTaskCount) : this(actions, null,
            null, null, maxTaskCount)
        {
        }

        public EditorTasks Start()
        {
            lock (SyncRoot)
            {
                if (started) return this;
                started = true;
            }

            try
            {
                OnStart?.Invoke();
            }
            catch
            {
            }

            lock (SyncRoot)
            {
                foreach (var task in parallel)
                    task.Start();
            }

            return this;
        }

        private static Task StartTask(Task task)
        {
            task.Start();
            return task;
        }

        public static EditorTasks ForEach<T>(IEnumerable<T> items, Action<T> itemAction,
            int maxTaskCount = DefaultMaxTaskCount, Action onStart = null, Action onComplete = null,
            Action<int, bool> onProcess = null)
        {
            return new EditorTasks(items.Select(item => new Action(() => itemAction(item))),
                onProcess,
                onStart, onComplete,
                maxTaskCount);
        }
    }
}