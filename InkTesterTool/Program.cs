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
        return 0;
    }
    else if (arg.Equals("--test")) { // Internal testing, for dev. Not to be confused with testVar.
        options.folder="tests";
        options.storyFile="test.ink";
        options.testRuns = 1000;
        //options.testVar = "Testing";
        csvOptions.outputFilePath="tests/report.csv";
    }
}

// ----- Test Ink -----
var tester = new Tester(options);
if (!tester.Run()) {
    Console.Error.WriteLine("Not tested.");
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
