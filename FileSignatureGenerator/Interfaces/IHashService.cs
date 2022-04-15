using System.Security.Cryptography;

namespace FileSignatureGenerator.Interfaces
{
    public interface IHashService
    {
        public byte[] ComputeSHA256Hash(HashAlgorithm hashAlgorithm, byte[] data);
    }
}