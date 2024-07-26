using InkTester;

var options = new Tester.Options();
var csvOptions = new CSVHandler.Options();

// ----- Simple Args -----
foreach (var arg in args)
{
    if (arg.StartsWith("--folder="))
        options.folder = arg.Substring(9);
    else if (arg.StartsWith("--storyFile="))
        options.storyFile = arg.Substring(12);
    else if (arg.StartsWith("--testVar="))
        options.testVar = arg.Substring(10);
    else if (arg.StartsWith("--runs="))
        options.testRuns = int.Parse(arg.Substring(7));
    else if (arg.StartsWith("--csv="))
        csvOptions.outputFilePath = arg.Substring(6);
    else if (arg.StartsWith("--maxSteps=")) {
        options.maxSteps = int.Parse(arg.Substring(11));
        options.maxStepsErrors = false; // Changing the default stops this being reported as an error.
    }
    else if (arg.Equals("--help") || arg.Equals("-h")) {
        Console.WriteLine("Ink Tester");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --folder=<folder> - Root working folder for Ink files, relative to current working dir.");
        Console.WriteLine("                      e.g. --folder=inkFiles/");
        Console.WriteLine("                      Default is the current working dir.");
        Console.WriteLine("  --storyFile=<file> - Ink file to test.");
        Console.WriteLine("                       e.g. --storyFile=start.ink");
        Console.WriteLine("  --runs=<num> - How many times to run the randomized test.");
        Console.WriteLine("                 e.g. --runs=1000");  
        Console.WriteLine("                 Default is 100.");      
        Console.WriteLine("  --csv=<csvFile> - Path to a CSV file to export, relative to working dir.");
        Console.WriteLine("                    e.g. --csv=output/report.csv");
        Console.WriteLine("                    Default is empty, so no CSV file will be generated.");
        Console.WriteLine("  --testVar=<varName> - Set this variable to TRUE. Useful for setting test data in Ink.");
        Console.WriteLine("                        e.g. --testVar=Testing");
        Console.WriteLine("  --maxSteps=<num> - How many steps to allow your ink story to take before ending. This avoids infinite loops and deals with stories that don't have an explicit ->END.");
        Console.WriteLine("                    e.g. --maxSteps=1000");
        Console.WriteLine("                    Default is 10000, to avoid infinite loops - but when using default, an error will be reported and testing will cease. If you specify your own maxSteps, this won't error.");
        return 0;
    }
    else if (arg.Equals("--test")) { // Internal testing, for dev. Not to be confused with testVar.
        options.folder="tests";
        options.storyFile="test.ink";
        options.testRuns = 1000;
        //options.maxSteps = 1000;
        //options.maxStepsErrors = false;
        //options.testVar = "Testing";
        csvOptions.outputFilePath="tests/report.csv";
    }
}

// ----- Test Ink -----
var tester = new Tester(options);
if (!tester.Run()) {
    Console.Error.WriteLine("Tests not completed.");
    return -1;
}
Console.WriteLine($"Tested.");

// ----- CSV Output -----
if (!String.IsNullOrEmpty(csvOptions.outputFilePath)) {
    var csvHandler = new CSVHandler(tester, csvOptions);
    if (!csvHandler.WriteReport()) {
        Console.Error.WriteLine("Report not written.");
        return -1;
    }
    Console.WriteLine($"CSV file written: {csvOptions.outputFilePath}");
}
else {
    Console.Error.WriteLine("No CSV file path supplied - use --csv=<filename.csv>");
}

return 0;
