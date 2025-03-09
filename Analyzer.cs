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

        _files.AddRange(Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories));

        if (_files.Count == 0)
        {
            Console.WriteLine("No C# files found.");
            return;
        }

        // Start HTML Report with Styling
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
            <h1>Code Review Report</h1>
            <table>
                <tr>
                    <th>File</th>
                    <th>Classes</th>
                    <th>Methods</th>
                    <th>Properties</th>
                    <th>Issues</th>
                </tr>");

        foreach (var file in _files)
        {
            AnalyzeFile(file);
        }

        _htmlReport.AppendLine("</table></body></html>");

        // Save report
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
        DetectLongMethods(root, issues);
        DetectMissingComments(root, issues);
        DetectPoorNaming(root, issues);
        DetectUnusedUsings(root, issues);

        // Add results to HTML table
        _htmlReport.AppendLine($"<tr><td>{Path.GetFileName(filePath)}</td>");
        _htmlReport.AppendLine($"<td>{(classNames.Any() ? string.Join(", ", classNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(methodNames.Any() ? string.Join(", ", methodNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(propertyNames.Any() ? string.Join(", ", propertyNames) : "None")}</td>");
        _htmlReport.AppendLine($"<td>{(issues.Any() ? string.Join("<br>", issues) : "No Issues Found")}</td></tr>");
    }

    private void DetectLongMethods(SyntaxNode root, List<string> issues)
{
    var longMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Where(m => (m.Body?.Statements.Count ?? (m.ExpressionBody != null ? 1 : 0)) > 20)
        .ToList();

    foreach (var method in longMethods)
    {
        issues.Add($"⚠️ Method '{method.Identifier.Text}' has more than 20 lines (consider refactoring).");
    }
}


    private void DetectMissingComments(SyntaxNode root, List<string> issues)
    {
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        foreach (var method in methods)
        {
            var trivia = method.GetLeadingTrivia();
          
          bool hasComments = trivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) 
                                || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

            if (!hasComments)
            {
                issues.Add($"⚠️ Method '{method.Identifier.Text}' lacks documentation/comments.");
            }
        }
    }

    private void DetectPoorNaming(SyntaxNode root, List<string> issues)
    {
        var methodNames = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(m => m.Identifier.Text);
        foreach (var method in methodNames)
        {
            if (!char.IsUpper(method[0]))  // PascalCase for methods
            {
                issues.Add($"⚠️ Method '{method}' should follow PascalCase naming convention.");
            }
        }

        var variableNames = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.Text);
        foreach (var variable in variableNames)
        {
            if (!char.IsLower(variable[0]))  // camelCase for variables
            {
                issues.Add($"⚠️ Variable '{variable}' should follows camelCase naming convention.");
            }
        }
    }

    private void DetectUnusedUsings(SyntaxNode root, List<string> issues)
    {
        var usingDirectives = root.DescendantNodes()
    .OfType<UsingDirectiveSyntax>()
    .Select(u => u.Name?.ToString() ?? "Unknown") // Fix: Handle null Name
    .ToList();

var identifiers = root.DescendantNodes()
    .OfType<IdentifierNameSyntax>()
    .Select(id => id.Identifier.Text ?? "Unknown") // Fix: Handle null Identifier
    .ToHashSet();

        foreach (var usingDirective in usingDirectives)
        {
            if (!identifiers.Contains(usingDirective))
            {
                issues.Add($"⚠️ Unused using directive: {usingDirective}");
            }
        }
    }
}
