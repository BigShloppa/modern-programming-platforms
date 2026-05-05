using System;
using System.Threading;

namespace CustomThreadPool
{
    internal class WorkerThread
    {
        private readonly DynamicThreadPool _pool;
        private readonly TimeSpan _idleTimeout;
        private readonly Thread _thread;
        private volatile bool _running = true;
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        public int ManagedThreadId => _thread.ManagedThreadId;

        public WorkerThread(DynamicThreadPool pool, TimeSpan idleTimeout)
        {
            _pool = pool;
            _idleTimeout = idleTimeout;
            _thread = new Thread(WorkerLoop);
            _thread.IsBackground = true;
        }

        public void Start()
        {
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            _stopEvent.Set();
            _thread.Join(1000);
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                bool dequeued = _pool.TryDequeue(out Action? task);
                if (dequeued && task != null)
                {
                    try
                    {
                        task();
                    }
                    catch (Exception ex)
                    {
                        string error = $"[Worker {ManagedThreadId}] Task failed: {ex.Message}";
                        _pool.LogStateFromWorker(error);
                    }
                    continue;
                }

                _pool.NotifyWorkerIdle(this);

                bool signaled = _stopEvent.WaitOne(_idleTimeout);
                if (signaled || !_running)
                    break;

                if (!_running) break;

                if (_pool.PendingTaskCount == 0)
                {
                    break;
                }
            }

            _pool.NotifyWorkerExit(this);
        }
    }
}