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
                
                // Group by FileName while preserving the original order
                var groupedVisitLog = _tester.VisitLog
                    .GroupBy(entry => entry.FileName)
                    .Select(group => new { FileName = group.Key, Entries = group.OrderBy(entry => entry.LineNumber).ToList() })
                    .ToList();

                // Flatten the groups back into a single list
                List<Tester.VisitEntry> sortedVisitLog = new ();
                foreach (var group in groupedVisitLog)
                {
                    sortedVisitLog.AddRange(group.Entries);
                }

                foreach(var entry in sortedVisitLog) {
                    
                    var textValue = entry.Text;
                    textValue = textValue.Replace("\"", "\"\"");

                    var percent = entry.PercentageVisits.ToString("F2", CultureInfo.InvariantCulture);
                    var line = $"\"{entry.FileName}\",{entry.LineNumber},\"{textValue}\",{entry.Visits},{percent}";
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

        public bool WriteOOCReport(bool isOOC = false) {

            string outputFilePath = Path.GetFullPath(_options.outputFilePath);

            try {
                StringBuilder output = new();
                output.AppendLine("Error,Last Good File,Last Good Line, Last Good Text");
                
                // Group by FileName while preserving the original order
                List<Tester.OOCEntry> groupedOOCLog = _tester.OOCLog
                .OrderBy(entry => entry.ErrorText)
                .ThenBy(entry => entry.LastGoodFileName)
                .ThenBy(entry => entry.LastGoodLineNumber)
                .ToList();

                foreach(var entry in groupedOOCLog) {
                    
                    var errorTextValue = entry.ErrorText;
                    errorTextValue = errorTextValue.Replace("\"", "\"\"");

                    var lastGoodTextValue = entry.LastGoodText;
                    lastGoodTextValue = lastGoodTextValue.Replace("\"", "\"\"");

                    var line = $"\"{errorTextValue}\",\"{entry.LastGoodFileName}\",{entry.LastGoodLineNumber},\"{lastGoodTextValue}\"";
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