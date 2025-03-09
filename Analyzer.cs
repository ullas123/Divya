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

    public void AnalyzeProject(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Invalid directory.");
            return;
        }

        // Collect all .cs files (C# only)
        _files.AddRange(Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories));

        if (_files.Count == 0)
        {
            Console.WriteLine("No C# files found.");
            return;
        }

        // Start HTML Report
        _htmlReport.AppendLine(@"
        <html>
        <head>
            <title>Code Review Report</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { text-align: center; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th { background-color: #add8e6; padding: 10px; border: 1px solid #ddd; width: 10%; }
                th:last-child { width: 60%; } /* Issues column takes 60% */
                td { padding: 10px; border: 1px solid #ddd; text-align: left; }
                tr:nth-child(even) { background-color: #f9f9f9; }
            </style>
        </head>
        <body>
            <h1>Code Review Report</h1>
            <table>
                <tr>
                    <th>File</th>
                    <th>Classes</th>
                    <th>Methods</th>
                    <th>Properties</th>
                    <th>Issues (Critical, High, Medium, Low)</th>
                </tr>");

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

        // Code Review Analysis
        List<string> issues = new List<string>();
        DetectSecurityRisks(root, issues);
        DetectPerformanceBottlenecks(root, issues);
        DetectCodeSmells(root, issues);
        DetectRefactoringPriorities(root, issues);

        // Add results to HTML table
        _htmlReport.AppendLine($"<tr><td>{Path.GetFileName(filePath)}</td>");
        _htmlReport.AppendLine($"<td>{(classNames.Any() ? string.Join(", ", classNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(methodNames.Any() ? string.Join(", ", methodNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(propertyNames.Any() ? string.Join(", ", propertyNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(issues.Any() ? string.Join("<br>", issues) : "No Issues Found")}</td></tr>");
    }

    // Detect hardcoded credentials, weak encryption, etc.
    private void DetectSecurityRisks(SyntaxNode root, List<string> issues)
    {
        var hardcodedStrings = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
            .Where(l => l.Token.ValueText.Contains("password") || l.Token.ValueText.Contains("secret"))
            .ToList();

        foreach (var item in hardcodedStrings)
        {
            issues.Add($"❗ CRITICAL: Hardcoded sensitive data detected: '{item.Token.ValueText}'.");
        }
    }

    // Detect performance bottlenecks such as inefficient LINQ usage
    private void DetectPerformanceBottlenecks(SyntaxNode root, List<string> issues)
    {
        var unnecessaryLinqCalls = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.ToString().Contains(".ToList().Where") || inv.ToString().Contains(".ToArray().Where"))
            .ToList();

        foreach (var call in unnecessaryLinqCalls)
        {
            issues.Add($"⚠️ HIGH: Unnecessary LINQ conversion detected, use direct filtering.");
        }
    }

    // Detect deep nesting, large parameter lists
    private void DetectCodeSmells(SyntaxNode root, List<string> issues)
    {
        var deeplyNestedIfs = root.DescendantNodes().OfType<IfStatementSyntax>()
            .Where(ifStmt => ifStmt.Ancestors().Count(a => a is IfStatementSyntax) > 2)
            .ToList();

        foreach (var ifStmt in deeplyNestedIfs)
        {
            issues.Add($"⚠️ MEDIUM: Deeply nested if-statements, consider refactoring.");
        }

        var methodsWithTooManyParams = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.ParameterList.Parameters.Count > 5)
            .ToList();

        foreach (var method in methodsWithTooManyParams)
        {
            issues.Add($"⚠️ MEDIUM: Method '{method.Identifier.Text}' has too many parameters.");
        }
    }

    // Prioritize refactoring issues
    private void DetectRefactoringPriorities(SyntaxNode root, List<string> issues)
    {
        var longMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => (m.Body?.Statements.Count ?? (m.ExpressionBody != null ? 1 : 0)) > 30)
            .ToList();

        foreach (var method in longMethods)
        {
            issues.Add($"⚠️ LOW: Method '{method.Identifier.Text}' is too long, consider refactoring.");
        }
    }
}
