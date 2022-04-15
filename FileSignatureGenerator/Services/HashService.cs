using System.Security.Cryptography;
using FileSignatureGenerator.Interfaces;

namespace FileSignatureGenerator.Services
{
    public sealed class HashService : IHashService
    {
        private object _locker = new();

        public byte[] ComputeSHA256Hash(HashAlgorithm hashAlgorithm, byte[] data)
        {
            byte[] hashArray;
            hashArray = hashAlgorithm.ComputeHash(data);
            return hashArray;
        }
    }
}