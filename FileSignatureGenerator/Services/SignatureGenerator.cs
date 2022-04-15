using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FileSignatureGenerator.Interfaces;

namespace FileSignatureGenerator.Services
{
    public sealed class SignatureGenerator: ISignatureGenerator
    {
        private readonly IHashService _hashService;
        private readonly IThreadPoolService _threadPoolService;

        private readonly object _locker = new();
        private readonly object _loggerLocker = new(); 

        [ThreadStatic]
        static SHA256 _sha256Hash;

        public SignatureGenerator(IHashService hashService, IThreadPoolService threadPoolService)
        {
            _hashService = hashService;
            _threadPoolService = threadPoolService;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<long, byte[]> GenerateSignature(string fileName, long blockSize, Action<string> loggerAction = null)
        {
            if (blockSize > int.MaxValue)
                throw new ArgumentException("Please specify smaller block size");

            var result = new ConcurrentDictionary<long, byte[]>();

            using FileStream sourceFile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            var blockIndex = 0;
            ConcurrentQueue<(int, byte[])> blocksToHash = new ConcurrentQueue<(int, byte[])>();
            var fileEnded = false;

            _threadPoolService.WithParallel(() =>
            {
                byte[] block = new byte[blockSize];
                int scopedIndex;
                int readBytesCount;
                lock (_locker)
                {
                    readBytesCount = sourceFile.Read(block, 0, (int)blockSize);

                    if (readBytesCount <= 0)
                    {
                        fileEnded = true;
                        return false;
                    }

                    scopedIndex = blockIndex++;
                    if (readBytesCount < blockSize)
                        block = block.Take(readBytesCount).ToArray();

                    blocksToHash.Enqueue((scopedIndex, block));
                }

                return true;
            });

            _threadPoolService.WithParallel(() =>
            {
                if (!blocksToHash.TryDequeue(out var item))
                {
                    return !fileEnded || !blocksToHash.IsEmpty;
                }

                _sha256Hash ??= SHA256.Create();
                var block = item.Item2;
                
                var blockHash = _hashService.ComputeSHA256Hash(_sha256Hash, block);

                result.TryAdd(item.Item1, blockHash);
                //LogValues(blockHash, item.Item1, loggerAction);
                return !fileEnded || !blocksToHash.IsEmpty;
            });

            _threadPoolService.Wait();

            return result;
        }

        private void LogValues(byte[] hashBytes, long blockIndex, Action<string> loggerAction)
        {
            if (loggerAction == null)
                return;

            var hashString = new StringBuilder();
            foreach (var hashByte in hashBytes)
                hashString.Append(hashByte.ToString("x2"));

            lock (_loggerLocker)
            {
                loggerAction(
                    $"{blockIndex} : {hashString}");
            }
        }
    }

}