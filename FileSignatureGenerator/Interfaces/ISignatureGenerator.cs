using System;
using System.Collections.Generic;

namespace FileSignatureGenerator.Interfaces
{
    public interface ISignatureGenerator
    {
        /// <summary>
        /// Generate signature for specified file based on provided block size.
        /// </summary>
        /// <param name="fileName">File path for which signature will be created</param>
        /// <param name="blockSize">Partition block size</param>
        /// <param name="loggerAction">Action for logging information, if provided will log additional info</param>
        /// <returns>Block number to it's hash value pairs</returns>
        public IReadOnlyDictionary<long, byte[]> GenerateSignature(string fileName, long blockSize, Action<string> loggerAction = null);
    }
}