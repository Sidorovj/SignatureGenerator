using System;

namespace FileSignatureGenerator.Interfaces
{
    public interface IThreadPoolService
    {
        void Wait();

        /// <summary>
        /// Executes action in parallel
        /// </summary>
        /// <param name="action">true, if action should continue executing</param>
        /// <returns></returns>
        void WithParallel(Func<bool> action);
    }
}