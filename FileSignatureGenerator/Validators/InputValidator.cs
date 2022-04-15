using System;
using System.IO;
using FileSignatureGenerator.Interfaces;

namespace FileSignatureGenerator.Validators
{
    public sealed class InputValidator: IInputValidator
    {
        /// <inheritdoc />
        public (string fileName, int blockSize) ValidateArguments(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                throw new InvalidOperationException("Please provide exactly 2 parameters: file path and block size");
            }

            var fileName = args[0];

            if (!File.Exists(fileName))
                throw new FileNotFoundException("File doesn't exist");

            if (!int.TryParse(args[1], out var blockSize))
            {
                throw new ArgumentException($"Can't parse '{args[1]}' to int type");
            }

            if (blockSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than 0");
            }

            return (fileName, blockSize);
        }
    }
}