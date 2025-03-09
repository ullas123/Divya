using System;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter C# project folder path: ");
        string folderPath = Console.ReadLine() ?? "";

        

        Analyzer analyzer = new Analyzer();
        analyzer.AnalyzeProject(folderPath);
    }
}
