using FileSignatureGenerator.Services;
using NUnit.Framework;
using SignatureGeneratorTests.TestServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SignatureGeneratorTests
{
    public class SignatureGeneratorTests
    {
        private const string FilePath75Gb = "D:/_temp/NVMe SAMSUNG MZALQ256HAJD-000L2 AL2QFXV7_full_b4_s1_v1.tib";
        private const string FilePath25Gb = "D:/_temp/NVMe SAMSUNG MZALQ256HAJD-000L2 AL2QFXV7_inc_b3_s4_v1.tib";
        private const string FilePath6Gb = "D:/_temp/NVMe SAMSUNG MZALQ256HAJD-000L2 AL2QFXV7_inc_b4_s2_v1.tib";
        private const string FilePath250Mb = "D:/investing/Warren Buffett MBA Talk.mp4";

        private SignatureGenerator _generator;

        [SetUp]
        public void SetupService()
        {
            _generator = new SignatureGenerator(new HashService(), new ThreadPoolService());
        }

        [Test]
        [TestCase(FilePath25Gb, int.MaxValue)]
        public void Should_throw_aggregate_exception_if_workers_fail_with_inner_exceptions(string filePath, long blockSize)
        {
            var exception = Assert.Throws<AggregateException>(() => DoTest(filePath, blockSize));

            var workersCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1;
            Assert.AreEqual(workersCount, exception?.InnerExceptions.Count);
        }

        [Test]
        [TestCase(FilePath25Gb, (long)int.MaxValue + 1)]
        public void Should_throw_argument_exception_for_too_big_block_size(string filePath, long blockSize)
        {
            Assert.Throws<ArgumentException>(() => DoTest(filePath, blockSize));
        }

        [Test]
        [TestCase(FilePath250Mb, 10000)]
        [TestCase(FilePath250Mb, 1000000)]
        public void Simple_one_threaded_generator_should_give_same_result(string filePath, long blockSize)
        {
            var simpleGenerator = new SimpleFileSignatureGenerator();

            var result = simpleGenerator.GenerateSignature(filePath, blockSize);

            AssertResults(filePath, blockSize, result);
        }

        [Test]
        [TestCase(FilePath25Gb, 100000)]
        [TestCase(FilePath25Gb, 1000000)]
        [TestCase(FilePath25Gb, int.MaxValue/2)]
        [TestCase(FilePath75Gb, 1000000)]
        public void Should_succeed_with_files_bigger_20_GB(string filePath, long blockSize)
        {
            var result = DoTest(filePath, blockSize);
            AssertResults(filePath, blockSize, result);
        }

        [Test]
        [TestCase(FilePath25Gb, 1000000)]
        public void Should_succeed_with_files_bigger_20_GB_repeated_test(string filePath, long blockSize)
        {
            for (var i =0 ;i<3; i++)
            {
                var result = DoTest(filePath, blockSize);

                AssertResults(filePath, blockSize, result);
            }
        }

        [Test]
        [TestCase(FilePath6Gb, 100000)]
        [TestCase(FilePath6Gb, 777777)]
        [TestCase(FilePath6Gb, 1000000)]
        public void Should_succeed_with_files_between_1_and_20_GB(string filePath, long blockSize)
        {
            var result = DoTest(filePath, blockSize);

            AssertResults(filePath, blockSize, result);
        }

        [Test]
        [TestCase(FilePath250Mb, 1000)]
        [TestCase(FilePath250Mb, 10000)]
        [TestCase(FilePath250Mb, 1000000)]
        [TestCase(FilePath250Mb, 100000000)]
        [TestCase(FilePath250Mb, 12345)]
        public void Should_succeed_with_files_about_200_MB(string filePath, long blockSize)
        {
            var result = DoTest(filePath, blockSize);

            AssertResults(filePath, blockSize, result);
        }

        [Test]
        [TestCase(FilePath6Gb)]
        /// Edge case, когда размер блока кратен размеру файла
        public void Should_succeed_when_file_size_is_multiple_of_block_size(string filePath)
        {
            using FileStream sourceFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long blockSize;
            if (sourceFile.Length % 3 == 0)
                blockSize = sourceFile.Length / 3;
            else if (sourceFile.Length % 4 == 0)
                blockSize = sourceFile.Length / 4;
            else if (sourceFile.Length % 5 == 0)
                blockSize = sourceFile.Length / 5;
            else throw new InvalidOperationException("Please select another file that can be divided by 3 or 4 or 5");

            TestContext.Out.WriteLine($"File size in MB = {sourceFile.Length / 1024 / 1024} ; block size in MB = {blockSize / 1024 / 1024}");

            var result = DoTest(filePath, blockSize);

            AssertResults(filePath, blockSize, result);
        }

        [Test]
        [TestCase(FilePath6Gb)]
        /// Edge case, когда размер блока кратен размеру файла
        public void Should_succeed_when_file_size_is_NOT_multiple_of_block_size(string filePath)
        {
            using FileStream sourceFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long blockSize;
            if (sourceFile.Length % 3 != 0)
                blockSize = sourceFile.Length / 3;
            else if (sourceFile.Length % 4 != 0)
                blockSize = sourceFile.Length / 4;
            else if (sourceFile.Length % 5 != 0)
                blockSize = sourceFile.Length / 5;
            else throw new InvalidOperationException("Please select another file that can not be divided by 3 or 4 or 5");

            TestContext.Out.WriteLine($"File size in MB = {sourceFile.Length / 1024 / 1024} ; block size in MB = {blockSize / 1024 / 1024}");

            var result = DoTest(filePath, blockSize);

            AssertResults(filePath, blockSize, result);
        }

        private IReadOnlyDictionary<long, byte[]> DoTest(string filePath, long blockSize)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var result = _generator.GenerateSignature(filePath, blockSize);
            sw.Stop();
            TestContext.Out.WriteLine($"Real algorithm took time: {(double)sw.ElapsedMilliseconds / 1000} seconds");

            return result;
        }
        
        private void AssertResults(string filePath, long blockSize, IReadOnlyDictionary<long, byte[]> actualResult)
        {
            var simpleGenerator = new SimpleFileSignatureGenerator();
            Stopwatch sw = Stopwatch.StartNew();
            var expectedResult = simpleGenerator.GenerateSignature(filePath, blockSize);
            sw.Stop();
            TestContext.Out.WriteLine($"Simple algorithm took time: {(double)sw.ElapsedMilliseconds / 1000}");

            if (expectedResult.Count != actualResult.Count)
                Assert.Fail($"Result blocks count is incorrect. Simple = {expectedResult.Count} ; actual = {actualResult.Count}");

            foreach (var blockNumber in expectedResult.Keys)
            {
                if (!actualResult.ContainsKey(blockNumber))
                    Assert.Fail($"Result doesn't contain block # {blockNumber}");

                Assert.AreEqual(expectedResult[blockNumber], actualResult[blockNumber],
                    $"Block number = {blockNumber + 1}, total blocks = {expectedResult.Count}");
            }
        }
    }
}