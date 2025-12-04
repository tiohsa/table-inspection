using System.Text.RegularExpressions;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Infrastructure.Services;

public class PatternAnalyzerService : IAnalysisService
{
    public Task<AnalysisResult> AnalyzeColumnAsync(List<string> data, string regexPattern)
    {
        var result = new AnalysisResult
        {
            SampleSize = data.Count,
            SampleValues = data.Take(10).ToList() // Keep a small preview
        };

        if (string.IsNullOrWhiteSpace(regexPattern))
        {
            result.MatchCount = 0;
            result.IsMatch = false;
            return Task.FromResult(result);
        }

        try
        {
            var regex = new Regex(regexPattern);
            int matches = 0;
            foreach (var item in data)
            {
                if (item != null && regex.IsMatch(item))
                {
                    matches++;
                }
            }
            result.MatchCount = matches;
            result.IsMatch = matches > 0; // Simple boolean if any match, or maybe threshold? Keeping it simple.
        }
        catch (Exception)
        {
            // Invalid regex
            result.MatchCount = 0;
            result.IsMatch = false;
        }

        return Task.FromResult(result);
    }

    public Task<List<string>> DetectPatternsAsync(List<string> data)
    {
        // Simple auto-detection strategy
        var patterns = new HashSet<string>();
        foreach (var item in data)
        {
            if (string.IsNullOrEmpty(item)) continue;

            if (Regex.IsMatch(item, @"^\d+$")) patterns.Add("Numeric");
            else if (Regex.IsMatch(item, @"^[a-zA-Z]+$")) patterns.Add("Alphabetic");
            else if (Regex.IsMatch(item, @"^[a-zA-Z0-9]+$")) patterns.Add("Alphanumeric");
            else if (Regex.IsMatch(item, @"^\d{4}-\d{2}-\d{2}")) patterns.Add("Date (ISO)");
            else if (item.Contains("@")) patterns.Add("Email-like");
        }
        return Task.FromResult(patterns.ToList());
    }
}
