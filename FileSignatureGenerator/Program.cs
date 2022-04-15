using System;
using FileSignatureGenerator.Interfaces;
using FileSignatureGenerator.Services;
using FileSignatureGenerator.Validators;

namespace FileSignatureGenerator
{
    class Program
    {
        private static readonly IInputValidator _inputValidator = new InputValidator();
        private static readonly IHashService _hashService = new HashService();
        private static readonly IThreadPoolService _threadPoolService = new  ThreadPoolService();
        private static readonly ISignatureGenerator _signatureGenerator = new SignatureGenerator(_hashService, _threadPoolService);

        /// <summary>
        /// Требуется написать консольную программу на C# для генерации сигнатуры указанного файла.
        /// Сигнатура генерируется следующим образом: исходный файл делится на блоки заданной длины (кроме последнего блока),
        /// для каждого блока вычисляется значение hash-функции SHA256, и вместе с его номером выводится в консоль.
        /// </summary>
        static void Main(string[] args)
        {
            // TODO: Add DI :)
            
            try
            {
                var (fileName, blockSize) = _inputValidator.ValidateArguments(args);

                var result = _signatureGenerator.GenerateSignature(fileName, blockSize, LogInfo);
            }
            catch (AggregateException aggregateException)
            {
                HandleAggregateException(aggregateException);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadKey();
        }

        private static void HandleAggregateException(AggregateException aggregateException)
        {
            var index = 1;
            if (aggregateException.InnerExceptions.Count > 1)
            {
                Console.WriteLine("Several errors occurred:");
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    Console.WriteLine($"{index++}: {innerException}");
                }
            }
            else if (aggregateException.InnerExceptions.Count == 1)
            {
                Console.WriteLine($"{aggregateException.InnerExceptions[0]}");
            }
            else
            {
                Console.WriteLine(aggregateException.ToString());
            }
        }

        private static void LogInfo(string info)
        {
            Console.WriteLine(info);
        }
    }
}
