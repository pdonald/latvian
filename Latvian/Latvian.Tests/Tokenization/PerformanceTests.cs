using System.Diagnostics;
using System.IO;
using System.Text;

using Latvian.Tokenization;

using NUnit.Framework;

namespace Latvian.Tests.Tokenization
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void Compile()
        {
            int count = 100;

            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < count; i++)
            {
                LatvianTokenizer tokenizer = new LatvianTokenizer();
            }
            timer.Stop();

            Debug.WriteLine("Compile: {0:0.000} ms", timer.ElapsedMilliseconds / count);
            Debug.WriteLine("Compile x{1}: {0:0.000} ms", timer.ElapsedMilliseconds, count);
        }

        [Test]
        public void Load()
        {
            string filename = Path.GetTempFileName();
            LatvianTokenizer tokenizer = new LatvianTokenizer();
            tokenizer.Save(filename);

            int count = 100;

            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < count; i++)
            {
                LatvianTokenizer tokenizer2 = new LatvianTokenizer(filename);
            }
            timer.Stop();

            Debug.WriteLine("Load: {0:0.000} ms", timer.ElapsedMilliseconds / count);
            Debug.WriteLine("Load x{1}: {0:0.000} ms", timer.ElapsedMilliseconds, count);
        }

        [Test]
        public void TokenizeString()
        {
            int count = 5;
            int megabytes = 10;
            int size = megabytes * 1024 * 1024;;
            
            string sourceText = File.ReadAllText(@"C:\Users\peteris\Desktop\darba likums.txt");
            StringBuilder sb = new StringBuilder(size);
            while (sb.Length < size)
                sb.Append(sourceText);
            string text = sb.ToString();

            LatvianTokenizer tokenizer = new LatvianTokenizer();

            Stopwatch timer = new Stopwatch();
            int tokenCount = 0;
            timer.Start();
            for (int i = 0; i < count; i++)
            {
                foreach (Token token in tokenizer.Tokenize(text))
                {
                    tokenCount++;
                }
            }
            timer.Stop();

            Assert.IsTrue(tokenCount > 0);

            Debug.WriteLine("Tokenize string ({1} MiB): {0:0.000} ms", timer.ElapsedMilliseconds / count, megabytes);
            Debug.WriteLine("Tokenize string: {0:0.000} MB/s", (megabytes * count) / timer.Elapsed.TotalSeconds, megabytes);
            Debug.WriteLine("Tokenize string: {0:0.000} tokens/s", tokenCount / timer.Elapsed.TotalSeconds);
        }

        [Test]
        public void TokenizeFile()
        {
            int count = 5;
            int megabytes = 10;
            int size = megabytes * 1024 * 1024; ;

            string sourceText = File.ReadAllText(@"C:\Users\peteris\Desktop\darba likums.txt");
            StringBuilder sb = new StringBuilder(size);
            while (sb.Length < size)
                sb.Append(sourceText);
            string text = sb.ToString();

            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, text);

            LatvianTokenizer tokenizer = new LatvianTokenizer();

            Stopwatch timer = new Stopwatch();
            int tokenCount = 0;
            timer.Start();
            for (int i = 0; i < count; i++)
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    foreach (Token token in tokenizer.Tokenize(reader))
                    {
                        tokenCount++;
                    }
                }
            }
            timer.Stop();

            Assert.IsTrue(tokenCount > 0);

            Debug.WriteLine("Tokenize file ({1} MiB): {0:0.000} ms", timer.ElapsedMilliseconds / count, megabytes);
            Debug.WriteLine("Tokenize file: {0:0.000} MB/s", (megabytes * count) / timer.Elapsed.TotalSeconds, megabytes);
            Debug.WriteLine("Tokenize file: {0:0.000} tokens/s", tokenCount / timer.Elapsed.TotalSeconds);
        }
    }
}
