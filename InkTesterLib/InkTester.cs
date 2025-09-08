using System.Text.RegularExpressions;
using Ink;
using Ink.Parsed;
using Ink.Runtime;

namespace InkTester
{
    public class Tester {

        public class Options
        {
            // Root folder. If empty, uses current working dir.
            public string folder = "";
            // File to test. Will automatically use all includes.
            public string storyFile = "";
            // How many runs of the game to test.
            public int testRuns = 1000;
            // If set, sets a variable called this to TRUE before running each test.
            public string testVar = "";
            // Max number of TURNS() allowed in a single story run.
            public int maxSteps = 10000;
            // Treat exceeded maxTurns as an error?
            public bool maxStepsErrors = true;
            // Running in Out of Content mode
            public bool ooc = false;
            // If set, limits the number of choices to this number
            public int maxChoices = -1;
        }
        private Options _options;


        private IFileHandler _fileHandler = new DefaultFileHandler();
        private bool _inkErrors = false;

        private int _lastValidLineNum = -1;
        private string _lastValidFile = "";
        private string _lastValidText = "";

        private bool _oocError = false;

        private string _previousCWD="";

        private Random _random = new Random();

        // Filename, Line Number, Line Count
        private Dictionary<string, Dictionary<int, int>> _visitWorkingLog = new();

        public Tester(Options? options = null) {
            _options = options ?? new Options();
        }

        public bool Run() {

            // ----- Set up environment -----
            // We'll restore this later.
            _previousCWD = Environment.CurrentDirectory;

            string folderPath = _options.folder;
            if (String.IsNullOrWhiteSpace(folderPath))
                folderPath = _previousCWD;
            folderPath = System.IO.Path.GetFullPath(folderPath);

            string inkFileName = _options.storyFile;
            if (String.IsNullOrWhiteSpace(inkFileName)) {
                Console.Error.WriteLine("Error - no storyFile specified.");
                return false;
            }

            string inkFilePath = System.IO.Path.Combine(folderPath, inkFileName);
            if (!File.Exists(inkFilePath)){
                Console.Error.WriteLine($"Error - can't find storyFile: {inkFilePath}");
                return false;
            }
            
            // Need this for InkParser to work properly with includes and such.
            Directory.SetCurrentDirectory(folderPath);

            bool success = ProcessFile(inkFilePath, inkFileName);

            // Restore current directory.
            Directory.SetCurrentDirectory(_previousCWD);

            return success;
        }

        private bool ProcessFile(string inkFilePath, string inkFileName) {
            
            // ----- Parse file... -----
            var content = _fileHandler.LoadInkFileContents(inkFilePath);
            if (content==null) {
                Console.Error.WriteLine($"Error - can't load storyFile: {inkFilePath}");
                return false;
            }

            InkParser parser = new InkParser(content, inkFileName, OnError, _fileHandler);
            var parsedStory = parser.Parse();
            if (_inkErrors) {
                Console.Error.WriteLine($"Error parsing ink file.");
                return false;
            }

            // Process parsed story
            if (!ProcessStory(parsedStory)) {
                return false;
            }

            return true;
        }

        private bool ProcessStory(Ink.Parsed.Story parsedStory) {

            Console.WriteLine("Processing story.");

            _visitWorkingLog.Clear();

            // Tag all the line numbers first (linking up content to line numbers/files)
            LineTagger tagger = new LineTagger();
            tagger.Tag(parsedStory);

            // Convert the parsed story into a runtime story.
            Ink.Runtime.Story story = parsedStory.ExportRuntime(OnError);

            if (story == null)
                return false;

            story.onError += OnError;
            story.allowExternalFunctionFallbacks = true;

            OOCLog.Clear();
            VisitLog.Clear();

            Console.WriteLine($"Starting {_options.testRuns} runs...");

            // Let's do all our test runs.
            for(int runNum=0;runNum<_options.testRuns;runNum++) {
                
                // Reset the story.
                story.ResetState();

                // If a --testVar was supplied, set it to true
                if (!String.IsNullOrWhiteSpace(_options.testVar)) {

                    try {
                        story.variablesState[_options.testVar] = true;
                    }
                    catch (Exception) // Seems there's no simple "does var exist" query, so use a catch.
                    {
                        Console.Error.WriteLine($"Ink variable '{_options.testVar}' does not exist, so can't set it. Check your --testVar variable exists in the Ink structure.");
                        return false;
                    }
                }

                Dictionary<string, HashSet<int>> runVisitLog = new();

                // Do a test run!
                if (!TestRun(story, tagger, runNum, runVisitLog))
                    return false;

                // Add that run log to the main log
                foreach (var kvp in runVisitLog) {
                    var fileName = kvp.Key;
                    var lineNums = kvp.Value;

                    if (!_visitWorkingLog.ContainsKey(fileName)) {
                        // This file needs adding to the log
                        var newFileVisitLog=new Dictionary<int, int>();
                        _visitWorkingLog[fileName]=newFileVisitLog;

                        // Fetch from the tag parser a list of all lines we care about for this file
                        // And set their visit count to 0
                        var expectedLineNums = tagger.GetLineNumsForFile(fileName);
                        foreach(var lineNum in expectedLineNums) {
                            newFileVisitLog[lineNum]=0;
                        }
                    }
                    
                    var fileVisitLog = _visitWorkingLog[fileName];

                    foreach(var lineNum in lineNums) {
                        // Add to the visit count.
                        if (!fileVisitLog.ContainsKey(lineNum))
                            fileVisitLog[lineNum]=0;
                        fileVisitLog[lineNum]=fileVisitLog[lineNum]+1;
                    }
                }
            }

            // Take what we've learned and build it into a simple log.
            BuildVisitLog();

            Console.WriteLine($"{_options.testRuns} runs done.");

            return true;
        }

        private bool TestRun(Ink.Runtime.Story story, LineTagger tagger, int runNum, Dictionary<string, HashSet<int>> runVisitLog) {

            Console.WriteLine($"Test run {runNum+1}...");

            int steps = 0;
            
            // Clear the out-of-content error
            _oocError = false;

            _lastValidLineNum = -1;
            _lastValidFile = "";

            while (story.canContinue) {
                
                while(story.canContinue) {
                    
                    if (steps>=_options.maxSteps) {
                        if (_options.maxStepsErrors) {
                            Console.Error.WriteLine("Exceeded max steps!");
                            return false;
                        }
                        else {
                            Console.WriteLine("Exceeded specified max steps - halting run.");
                            return true;
                        }
                    }
                    steps++;

                    string text = story.Continue();

                    if (_options.ooc && _oocError)
                        return true;

                    var tags = story.currentTags;
                    // Use the tag we stuck into that line to get the parsed version of the object
                    // We could have used debugMetadata in runtime but it's flaky in the runtime version and can be wrong
                    var pObj = tagger.GetParsedObjectFromTags(tags);
                    if (pObj!=null) {

                        _lastValidLineNum = pObj.debugMetadata.startLineNumber;
                        _lastValidFile = pObj.debugMetadata.fileName;
                        _lastValidText = text;

                        if (!_options.ooc) {
                            if (!runVisitLog.ContainsKey(_lastValidFile)) {
                                // This file needs adding to the log
                                var newFileVisitLog=new HashSet<int>();
                                runVisitLog[_lastValidFile]=newFileVisitLog;
                            }
                            runVisitLog[_lastValidFile].Add(_lastValidLineNum);
                        }
                    }

                    if (_inkErrors)
                        return false;
                }

                // No more choices? End of story!
                if (story.currentChoices.Count==0)
                {
                    // Done.
                    break;
                }

                // Pick a random choice!
                int choiceCount = story.currentChoices.Count;
                if (_options.maxChoices>0 && choiceCount>_options.maxChoices)
                    choiceCount = _options.maxChoices;
                story.ChooseChoiceIndex(_random.Next(choiceCount));
            }

            return !_inkErrors;
        }
        

        void OnError(string message, ErrorType type)
        {
            if (type==ErrorType.Author)
                return;
            if (type==ErrorType.Warning) {
                Console.Error.WriteLine("Ink Warning: "+message);
                return;
            }

            if (_options.ooc && (message.Contains("ran out of content.") || message.Contains("end of content."))){

                _oocError = true;

                // We hit an Out of Content error. Log it, bail out early.
                var oocEntry = new OOCEntry {
                    ErrorText = message.Substring("RUNTIME ERROR: ".Length).Trim(),
                    LastGoodFileName = _lastValidFile,
                    LastGoodLineNumber = _lastValidLineNum,
                    LastGoodText = _lastValidText.Trim()
                };
                if (!OOCLog.Any(entry => 
                    entry.ErrorText == oocEntry.ErrorText &&
                    entry.LastGoodFileName == oocEntry.LastGoodFileName &&
                    entry.LastGoodLineNumber == oocEntry.LastGoodLineNumber &&
                    entry.LastGoodText == oocEntry.LastGoodText))
                {
                    OOCLog.Add(oocEntry);
                }

                return; // Don't treat it as an error, return to the loop which will log it and exit the run.
            }
            _inkErrors = true;
            Console.Error.WriteLine("Ink Error: "+message);
        }

        // This is what gets stored in the visit log
        public class VisitEntry {
            public required string FileName { get; set; }
            public int LineNumber { get; set; }
            public required string Text { get; set; }
            public int Visits { get; set; }
            public float PercentageVisits { get; set; }
        }
        public List<VisitEntry> VisitLog = new();

        public class OOCEntry {
            public required string ErrorText {get; set;}
            public required string LastGoodFileName {get; set;}
            public int LastGoodLineNumber {get;set;}
            public required string LastGoodText {get;set;}
        }
        public List<OOCEntry> OOCLog = new();

        private Dictionary<string, string[]> _buildFileContent = new();

        // After it's all over, collate the log into something readable.
        private void BuildVisitLog() {
            VisitLog.Clear();
            _buildFileContent.Clear();

            foreach(var fileKvp in _visitWorkingLog) {
                string fileName = fileKvp.Key;
                foreach(var lineKvp in fileKvp.Value) {
                    int lineNumber = lineKvp.Key;
                    int lineCount = lineKvp.Value;

                    if (_options.ooc && lineCount==0)
                        continue;

                    if (!_buildFileContent.ContainsKey(fileName)) {
                        var content = _fileHandler.LoadInkFileContents(fileName);
                        _buildFileContent[fileName] = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    }

                    var lines = _buildFileContent[fileName];

                    var visitEntry = new VisitEntry
                    {
                        FileName = fileName,
                        LineNumber = lineNumber,
                        Text = lines[lineNumber-1],
                        Visits = lineCount,
                        PercentageVisits = (float)lineCount * 100.0f / (float)_options.testRuns
                    };
                    VisitLog.Add(visitEntry);
                }
            }
        }
    }
}