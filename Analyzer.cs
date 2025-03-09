using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Analyzer
{
    private readonly List<string> _files = new();
    private readonly StringBuilder _htmlReport = new();
    private int _totalIssues = 0;
    private int _totalClasses = 0;
    private int _totalMethods = 0;
    private int _totalProperties = 0;

    private Dictionary<string, List<string>> sectionAnalysis = new();

    public void AnalyzeProject(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Invalid directory.");
            return;
        }

        string projectName = new DirectoryInfo(folderPath).Name;
        _files.AddRange(Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories));

        if (_files.Count == 0)
        {
            Console.WriteLine("No C# files found.");
            return;
        }

        // Initialize Section Analysis Dictionary
        sectionAnalysis["Security Risks"] = new List<string>();
        sectionAnalysis["Performance Bottlenecks"] = new List<string>();
        sectionAnalysis["Code Smells"] = new List<string>();
        sectionAnalysis["Refactoring Priorities"] = new List<string>();

        // Start HTML Report
        _htmlReport.AppendLine(@"
        <html>
        <head>
            <title>Code Review Report</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { text-align: center; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th { background-color: #add8e6; padding: 10px; border: 1px solid #ddd; }
                td { padding: 10px; border: 1px solid #ddd; text-align: left; }
                tr:nth-child(even) { background-color: #f9f9f9; }
            </style>
        </head>
        <body>
            <h1>Code Review Report</h1>");

        // Overall Summary Table
        _htmlReport.AppendLine($@"
            <h2>Overall Summary</h2>
            <table>
                <tr><th>Project Name</th><td>{projectName}</td></tr>
                <tr><th>Total Files</th><td>{_files.Count}</td></tr>
                <tr><th>Total Classes</th><td>{_totalClasses}</td></tr>
                <tr><th>Total Methods</th><td>{_totalMethods}</td></tr>
                <tr><th>Total Properties</th><td>{_totalProperties}</td></tr>
                <tr><th>Total Issues Found</th><td>{_totalIssues}</td></tr>
            </table>");

        // Section Analysis Table
        _htmlReport.AppendLine(@"
            <h2>Section Analysis</h2>
            <table>
                <tr><th>Category</th><th>Overview</th><th>Findings</th><th>Proposed Solution</th></tr>");

        foreach (var category in sectionAnalysis)
        {
            string findings = category.Value.Count > 0 ? string.Join("<br>", category.Value) : "No issues found";
            _htmlReport.AppendLine($@"
                <tr>
                    <td>{category.Key}</td>
                    <td>Analysis of {category.Key} across the project</td>
                    <td>{findings}</td>
                    <td>Refactor or optimize based on severity</td>
                </tr>");
        }
        _htmlReport.AppendLine("</table>");

        // Detailed File Analysis Table
        _htmlReport.AppendLine(@"
            <h2>Detailed Analysis</h2>
            <table>
                <tr><th>File</th><th>Classes</th><th>Methods</th><th>Properties</th><th>Issues</th></tr>");

        foreach (var file in _files)
        {
            AnalyzeFile(file);
        }

        _htmlReport.AppendLine("</table></body></html>");
        File.WriteAllText("CodeReviewReport.html", _htmlReport.ToString());
        Console.WriteLine("Analysis completed. Report saved as CodeReviewReport.html");
    }

    private void AnalyzeFile(string filePath)
    {
        string code = File.ReadAllText(filePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();

        var classNames = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c => c.Identifier.Text).ToList();
        var methodNames = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(m => m.Identifier.Text).ToList();
        var propertyNames = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(p => p.Identifier.Text).ToList();

        _totalClasses += classNames.Count;
        _totalMethods += methodNames.Count;
        _totalProperties += propertyNames.Count;

        // Code Review Analysis
        List<string> issues = new List<string>();
        DetectSecurityRisks(root, issues);
        DetectPerformanceBottlenecks(root, issues);
        DetectCodeSmells(root, issues);
        DetectRefactoringPriorities(root, issues);

        _totalIssues += issues.Count;

        _htmlReport.AppendLine($"<tr><td>{Path.GetFileName(filePath)}</td>");
        _htmlReport.AppendLine($"<td>{(classNames.Any() ? string.Join(", ", classNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(methodNames.Any() ? string.Join(", ", methodNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(propertyNames.Any() ? string.Join(", ", propertyNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(issues.Any() ? string.Join("<br>", issues) : "No Issues Found")}</td></tr>");
    }

    private void DetectSecurityRisks(SyntaxNode root, List<string> issues)
    {
        var hardcodedStrings = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
            .Where(l => l.Token.ValueText.Contains("password") || l.Token.ValueText.Contains("secret"))
            .ToList();

        foreach (var item in hardcodedStrings)
        {
            string issue = $"❗ Hardcoded sensitive data detected: '{item.Token.ValueText}'.";
            issues.Add(issue);
            sectionAnalysis["Security Risks"].Add(issue);
        }
    }

    private void DetectPerformanceBottlenecks(SyntaxNode root, List<string> issues)
    {
        var unnecessaryLinqCalls = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.ToString().Contains(".ToList().Where") || inv.ToString().Contains(".ToArray().Where"))
            .ToList();

        foreach (var call in unnecessaryLinqCalls)
        {
            string issue = $"⚠️ Unnecessary LINQ conversion detected.";
            issues.Add(issue);
            sectionAnalysis["Performance Bottlenecks"].Add(issue);
        }
    }

    private void DetectCodeSmells(SyntaxNode root, List<string> issues)
    {
        var methodsWithTooManyParams = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.ParameterList.Parameters.Count > 5)
            .ToList();

        foreach (var method in methodsWithTooManyParams)
        {
            string issue = $"⚠️ Method '{method.Identifier.Text}' has too many parameters.";
            issues.Add(issue);
            sectionAnalysis["Code Smells"].Add(issue);
        }
    }

    private void DetectRefactoringPriorities(SyntaxNode root, List<string> issues)
    {
        // Add future refactoring detection logic here
    }
}
