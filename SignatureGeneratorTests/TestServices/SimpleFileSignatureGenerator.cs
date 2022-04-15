using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FileSignatureGenerator.Interfaces;
using FileSignatureGenerator.Services;

namespace SignatureGeneratorTests.TestServices
{
    public class SimpleFileSignatureGenerator
    {
        private IHashService _hashService = new HashService();

        public IReadOnlyDictionary<long, byte[]> GenerateSignature(string fileName, long blockSize)
        {
            var result = new ConcurrentDictionary<long, byte[]>();

            using SHA256 sha256Hash = SHA256.Create();
            using FileStream sourceFile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var block = new byte[blockSize];
            var blockIndex = 0;
            var readBytesCount = 0;

            while ((readBytesCount = sourceFile.Read(block, 0, (int)blockSize)) > 0)
            {
                if (readBytesCount < blockSize)
                    block = block.Take(readBytesCount).ToArray();
                GenerateSignatureForBlock(blockIndex, block, result, sha256Hash);
                blockIndex++;
                block = new byte[blockSize];
            }

            return result;
        }

        private void GenerateSignatureForBlock(int blockIndex, byte[] block, ConcurrentDictionary<long, byte[]> result, HashAlgorithm hashAlgorithm)
        {
            var blockHash = _hashService.ComputeSHA256Hash(hashAlgorithm, block);

            // NOTE: If file is too large comment out next line
            result.TryAdd(blockIndex, blockHash);
        }
    }
}