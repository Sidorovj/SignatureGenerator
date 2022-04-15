namespace FileSignatureGenerator.Interfaces
{
    public interface IInputValidator
    {
        /// <summary>
        /// Validates arguments for creating a signature for file
        /// </summary>
        public (string fileName, int blockSize) ValidateArguments(string[] args);
    }
}