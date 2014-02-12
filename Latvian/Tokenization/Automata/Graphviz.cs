// Copyright 2014 Pēteris Ņikiforovs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Latvian.Tokenization.Automata
{
    class Graphviz
    {
        private const string DefaultPath = @"C:\Program Files (x86)\Graphviz2.36\bin";
        private StringBuilder sb = new StringBuilder();

        public Graphviz(string path = DefaultPath)
        {
            Path = path;
            LeftToRight = true;
        }

        public string Path { get; set; }
        public bool LeftToRight { get; set; }

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
            string direction = LeftToRight ? @"rankdir=""LR""; " : "";
            return "digraph g { " + direction + Environment.NewLine + sb.ToString() + "}";
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
                else
                {
                    throw new Exception("Could not launch Graphviz");
                }
            }
        }
    }
}
#endif
