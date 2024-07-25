using System.Globalization;
using System.Text;

namespace InkTester
{
    // Exports the report out to a CSV file.
    public class CSVHandler {

        public class Options {
            public string outputFilePath = "";
        }

        private Options _options;
        private Tester _tester;

        public CSVHandler(Tester tester, Options? options = null) {
            _tester = tester;
            _options = options ?? new Options();
        }

        public bool WriteReport() {

            string outputFilePath = Path.GetFullPath(_options.outputFilePath);

            try {
                StringBuilder output = new();
                output.AppendLine("File,Line,Text,Visit Count,Visit %");

                foreach(var entry in _tester.VisitLog) {
                    
                    var textValue = entry.Text;
                    textValue = textValue.Replace("\"", "\"\"");

                    var percent = entry.PercentageVisits.ToString("F2", CultureInfo.InvariantCulture);

                    var line = $"{entry.FileName},{entry.LineNumber},\"{textValue}\",{entry.Visits},{percent}";
                    output.AppendLine(line);
                }

                string fileContents = output.ToString();
                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out CSV file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}