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

            File.Delete(filename);
        }

        [Test]
        public void TokenizeString()
        {
            int count = 5;
            int mebibytes = 10;
            int size = mebibytes * 1024 * 1024;
            
            string text = DarbaLikums(size);
            Assert.AreEqual(size, text.Length);

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

            Debug.WriteLine("Tokenize string ({1} MiB): {0:0.000} ms", timer.ElapsedMilliseconds / count, mebibytes);
            Debug.WriteLine("Tokenize string: {0:0.000} MB/s", (mebibytes * count) / timer.Elapsed.TotalSeconds, mebibytes);
            Debug.WriteLine("Tokenize string: {0:0.000} tokens/s", tokenCount / timer.Elapsed.TotalSeconds);
        }

        [Test]
        public void TokenizeFile()
        {
            int count = 5;
            int mebibytes = 10;
            int size = mebibytes * 1024 * 1024; ;

            string text = DarbaLikums(size);
            Assert.AreEqual(size, text.Length);

            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, text);
            Assert.AreEqual(size, File.ReadAllText(filename).Length);

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

            Debug.WriteLine("Tokenize file ({1} MiB): {0:0.000} ms", timer.ElapsedMilliseconds / count, mebibytes);
            Debug.WriteLine("Tokenize file: {0:0.000} MB/s", (mebibytes * count) / timer.Elapsed.TotalSeconds, mebibytes);
            Debug.WriteLine("Tokenize file: {0:0.000} tokens/s", tokenCount / timer.Elapsed.TotalSeconds);

            File.Delete(filename);
        }

        private string DarbaLikums(int? desiredLength = null)
        {
            string resourceName = "Latvian.Tests.Resources.DarbaLikums.txt";
            string text = null;
            using (StreamReader reader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)))
                text = reader.ReadToEnd();

            if (desiredLength == null || text.Length == desiredLength)
                return text;
            if (text.Length > desiredLength) 
                return text.Substring(0, desiredLength.Value);

            StringBuilder sb = new StringBuilder(desiredLength.Value);
            while (sb.Length < desiredLength.Value)
                sb.Append(text.Substring(0, System.Math.Min(text.Length, desiredLength.Value - sb.Length)));
            return sb.ToString();
        }
    }
}
