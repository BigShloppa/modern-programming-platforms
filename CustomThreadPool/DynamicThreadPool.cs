using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CustomThreadPool
{
    public class DynamicThreadPool : IDisposable
    {
        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly TimeSpan _idleTimeout;
        private readonly TimeSpan _queueWaitThreshold;

        private readonly ConcurrentQueue<Action> _taskQueue = new ConcurrentQueue<Action>();
        private readonly List<WorkerThread> _workers = new List<WorkerThread>();
        private readonly object _workersLock = new object();

        private bool _disposed = false;
        private int _queueWaitCount = 0;
        private readonly AutoResetEvent _taskAvailable = new AutoResetEvent(false);
        private readonly Thread _monitorThread;

        public event Action<string>? OnStateChanged;

        public int CurrentThreadCount
        {
            get
            {
                lock (_workersLock)
                    return _workers.Count;
            }
        }

        public int PendingTaskCount => _taskQueue.Count;

        public DynamicThreadPool(int minThreads, int maxThreads, int idleTimeoutSeconds = 5, int queueWaitThresholdSeconds = 1)
        {
            if (minThreads < 1) minThreads = 1;
            if (maxThreads < minThreads) maxThreads = minThreads;
            _minThreads = minThreads;
            _maxThreads = maxThreads;
            _idleTimeout = TimeSpan.FromSeconds(idleTimeoutSeconds);
            _queueWaitThreshold = TimeSpan.FromSeconds(queueWaitThresholdSeconds);

            for (int i = 0; i < _minThreads; i++)
            {
                CreateWorker();
            }

            _monitorThread = new Thread(MonitorLoop);
            _monitorThread.IsBackground = true;
            _monitorThread.Start();

            LogState("Pool initialized");
        }

        private void CreateWorker()
        {
            var worker = new WorkerThread(this, _idleTimeout);
            lock (_workersLock)
            {
                _workers.Add(worker);
            }
            worker.Start();
            LogState($"Worker created, total: {CurrentThreadCount}");
        }

        private void RemoveWorker(WorkerThread worker)
        {
            lock (_workersLock)
            {
                _workers.Remove(worker);
            }
            LogState($"Worker removed, total: {CurrentThreadCount}");
        }

        public void EnqueueTask(Action task)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DynamicThreadPool));

            _taskQueue.Enqueue(task);
            _taskAvailable.Set();
            LogState($"Task enqueued. Pending: {PendingTaskCount}");
        }

        public int EnqueueTasks(IEnumerable<Action> tasks)
        {
            int count = 0;
            foreach (var task in tasks)
            {
                EnqueueTask(task);
                count++;
            }
            return count;
        }

        private void MonitorLoop()
        {
            while (!_disposed)
            {
                Thread.Sleep(500);
                if (_disposed) break;

                int pending = PendingTaskCount;
                int currentWorkers = CurrentThreadCount;

                if (pending > 0)
                {
                    Interlocked.Increment(ref _queueWaitCount);
                    if (_queueWaitCount * 0.5 >= 2 && currentWorkers < _maxThreads)
                    {
                        lock (_workersLock)
                        {
                            if (currentWorkers < _maxThreads)
                            {
                                CreateWorker();
                            }
                        }
                        Interlocked.Exchange(ref _queueWaitCount, 0);
                    }
                }
                else
                {
                    Interlocked.Exchange(ref _queueWaitCount, 0);
                }

                bool needMore = pending > currentWorkers && currentWorkers < _maxThreads;
                if (needMore)
                {
                    lock (_workersLock)
                    {
                        if (currentWorkers < _maxThreads)
                        {
                            int toCreate = Math.Min(_maxThreads - currentWorkers, 2);
                            for (int i = 0; i < toCreate; i++)
                                CreateWorker();
                        }
                    }
                }
            }
        }

        internal bool TryDequeue(out Action? task)
        {
            return _taskQueue.TryDequeue(out task);
        }

        internal void NotifyWorkerIdle(WorkerThread worker)
        {
            LogState($"Worker idle: {worker.ManagedThreadId}");
        }

        internal void NotifyWorkerExit(WorkerThread worker)
        {
            RemoveWorker(worker);
            if (CurrentThreadCount < _minThreads && !_disposed)
            {
                CreateWorker();
            }
        }

        internal void LogStateFromWorker(string message)
        {
            LogState(message);
        }

        private void LogState(string message)
        {
            string state = $"[{DateTime.Now:HH:mm:ss.fff}] Pool: {message} | Threads={CurrentThreadCount}, Queue={PendingTaskCount}";
            OnStateChanged?.Invoke(state);
        }

        public void WaitForAllTasks()
        {
            while (PendingTaskCount > 0 || CurrentThreadCount > 0)
            {
                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _taskAvailable.Set();
            _monitorThread.Join(2000);
            lock (_workersLock)
            {
                foreach (var w in _workers)
                {
                    w.Stop();
                }
            }
            LogState("Pool disposed");
        }
    }
}