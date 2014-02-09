#if DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Latvian.Tokenization.Automata
{
    class Graphviz
    {
        private StringBuilder sb = new StringBuilder();

        public Graphviz(string path = null)
        {
            Path = path ?? @"C:\Program Files (x86)\Graphviz2.36\bin";
        }

        public string Path
        {
            get;
            set;
        }

        public void AddShape(string name, string shape)
        {
            sb.AppendFormat(@"""{0}"" [shape={1}]", Escape(name), shape);
            sb.AppendLine();
        }

        public void AddTransition(string fromName, string toName)
        {
            sb.AppendFormat(@"""{0}"" -> ""{1}""", Escape(fromName), Escape(toName));
            sb.AppendLine();
        }

        public void AddTransition(string fromName, string toName, string label)
        {
            sb.AppendFormat(@"""{0}"" -> ""{1}"" [label=""{2}""]", Escape(fromName), Escape(toName), Escape(label));
            sb.AppendLine();
        }

        private string Escape(string value)
        {
            return value.Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t")
                        .Replace("\"", "\\\"");
        }

        public override string ToString()
        {
            return "digraph g {" + Environment.NewLine + sb.ToString() + "}";
        }

        public void SaveImage(string filename)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = Path != null ? (!Path.EndsWith("dot") ? System.IO.Path.Combine(Path, "dot") : "") : "dot";
                process.StartInfo.Arguments = "-Tpng -o " + filename;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (process.Start())
                {
                    using (StreamWriter writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
                        writer.Write(ToString());

                    string error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(error))
                        throw new Exception(error);
                }
            }
        }
    }
}
#endif
