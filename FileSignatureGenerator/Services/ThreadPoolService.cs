using System;
using System.Collections.Concurrent;
using System.Threading;
using FileSignatureGenerator.Interfaces;

namespace FileSignatureGenerator.Services
{
    public sealed class ThreadPoolService : IThreadPoolService
    {
        /// <summary>
        /// 1 participant will be invoked in WaitForAll
        /// </summary>
        private readonly Barrier _barrier = new(1);
        private readonly object _locker = new();
        private readonly int _maxThreadsCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount/2 - 1 : 1;
        private readonly ConcurrentBag<Exception> _jobExceptions = new();

        private bool _shouldFinish;
        private int _threadCountInWork;

        public void Wait()
        {
            _shouldFinish = true;
            if (_threadCountInWork > 0)
            {
                _barrier.SignalAndWait();
            }

            _shouldFinish = false;

            if (_jobExceptions.Count > 0)
                throw new AggregateException(_jobExceptions);
        }

        public void WithParallel(Func<bool> func)
        {
            for (var i = 0; i < _maxThreadsCount; i++)
            {
                var thread = new Thread(() =>
                {
                    WithExceptionHandling(() =>
                    {
                        while (func())
                        {
                        }
                    });
                });

                thread.Start();
            }
        }

        private void WithExceptionHandling(Action action)
        {
            bool havePut = false;
            try
            {
                PutThreadToWork();
                havePut = true;

                action();
            }
            catch (Exception ex)
            {
                _jobExceptions.Add(ex);
            }
            finally
            {
                if (havePut)
                    ReleaseThread();
            }
        }

        private void PutThreadToWork()
        {
            _barrier.AddParticipant();
            lock (_locker)
            {
                _threadCountInWork++;
            }
        }

        private void ReleaseThread()
        {
            lock (_locker)
            {
                _threadCountInWork--;
            }

            try
            {
                if (_shouldFinish)
                {
                    _barrier.SignalAndWait();
                }

                _barrier.RemoveParticipant();
            }
            catch (Exception ex)
            {
                _jobExceptions.Add(ex);
            }
        }
    }
}