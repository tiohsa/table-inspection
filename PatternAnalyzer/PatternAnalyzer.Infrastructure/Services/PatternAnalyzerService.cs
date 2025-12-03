using System.Text.RegularExpressions;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;

namespace PatternAnalyzer.Infrastructure.Services;

public class PatternAnalyzerService : IPatternAnalyzer
{
    private readonly IEnumerable<IAnalysisStrategy> _strategies;

    public PatternAnalyzerService(IEnumerable<IAnalysisStrategy> strategies)
    {
        _strategies = strategies;
    }

    public AnalysisResult Analyze(IEnumerable<object> data, AnalysisRequest request)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(request.AnalysisType));
        if (strategy == null)
        {
            return new AnalysisResult
            {
                ColumnName = request.Column,
                Success = false,
                Message = "No suitable strategy found."
            };
        }

        return strategy.Execute(data, request);
    }
}

public class PrefixAnalysisStrategy : IAnalysisStrategy
{
    public bool CanHandle(AnalysisType type) => type == AnalysisType.Prefix;

    public AnalysisResult Execute(IEnumerable<object> data, AnalysisRequest request)
    {
        var list = data.Select(x => x.ToString() ?? "").ToList();
        var result = new AnalysisResult
        {
            ColumnName = request.Column,
            AnalysisType = AnalysisType.Prefix,
            TotalRowsAnalyzed = list.Count
        };

        if (list.Count == 0)
        {
            result.Success = true;
            result.Message = "No data to analyze.";
            return result;
        }

        string prefix = list[0];
        foreach (var item in list.Skip(1))
        {
            int len = 0;
            while (len < prefix.Length && len < item.Length && prefix[len] == item[len])
            {
                len++;
            }
            prefix = prefix.Substring(0, len);
            if (prefix.Length == 0) break;
        }

        result.CommonPrefix = prefix;
        result.MatchCount = list.Count(x => x.StartsWith(prefix)); // Should be all if logic holds, but useful verification
        result.Success = true;
        result.Message = string.IsNullOrEmpty(prefix) ? "No common prefix found." : $"Common prefix: '{prefix}'";

        return result;
    }
}

public class RegexAnalysisStrategy : IAnalysisStrategy
{
    public bool CanHandle(AnalysisType type) => type == AnalysisType.Regex;

    public AnalysisResult Execute(IEnumerable<object> data, AnalysisRequest request)
    {
        var list = data.Select(x => x.ToString() ?? "").ToList();
        var result = new AnalysisResult
        {
            ColumnName = request.Column,
            AnalysisType = AnalysisType.Regex,
            TotalRowsAnalyzed = list.Count
        };

        if (string.IsNullOrWhiteSpace(request.RegexPattern))
        {
            result.Success = false;
            result.Message = "Regex pattern is empty.";
            return result;
        }

        try
        {
            var regex = new Regex(request.RegexPattern);
            int matchCount = 0;
            var violations = new List<string>();

            foreach (var item in list)
            {
                if (regex.IsMatch(item))
                {
                    matchCount++;
                }
                else
                {
                    if (violations.Count < 5) violations.Add(item);
                }
            }

            result.MatchCount = matchCount;
            result.SampleViolations = violations;
            result.Success = true;
            result.Message = $"Matched {matchCount}/{list.Count} rows.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Invalid Regex: {ex.Message}";
        }

        return result;
    }
}

public class SequenceAnalysisStrategy : IAnalysisStrategy
{
    public bool CanHandle(AnalysisType type) => type == AnalysisType.Sequence;

    public AnalysisResult Execute(IEnumerable<object> data, AnalysisRequest request)
    {
        // Try to parse as long
        var validNumbers = new List<long>();
        int total = 0;
        foreach (var item in data)
        {
            total++;
            if (long.TryParse(item.ToString(), out long val))
            {
                validNumbers.Add(val);
            }
        }

        // Ensure sorted for sequence check? Or input order?
        // Usually sequence implies order.

        var result = new AnalysisResult
        {
            ColumnName = request.Column,
            AnalysisType = AnalysisType.Sequence,
            TotalRowsAnalyzed = total,
            MatchCount = validNumbers.Count
        };

        if (validNumbers.Count < 2)
        {
            result.IsSequential = true; // Trivial
            result.Success = true;
            return result;
        }

        bool isSequential = true;
        for (int i = 0; i < validNumbers.Count - 1; i++)
        {
            if (validNumbers[i + 1] != validNumbers[i] + 1)
            {
                isSequential = false;
                result.SampleViolations.Add($"{validNumbers[i]} -> {validNumbers[i+1]}");
                if (result.SampleViolations.Count >= 5) break;
            }
        }

        result.IsSequential = isSequential;
        result.Success = true;
        result.Message = isSequential ? "Data is sequential." : "Data is not sequential.";

        return result;
    }
}

public class AutoAnalysisStrategy : IAnalysisStrategy
{
    private readonly IEnumerable<IAnalysisStrategy> _strategies;

    // We inject other strategies to try them
    public AutoAnalysisStrategy(IEnumerable<IAnalysisStrategy> strategies)
    {
        _strategies = strategies;
    }

    public bool CanHandle(AnalysisType type) => type == AnalysisType.Auto;

    public AnalysisResult Execute(IEnumerable<object> data, AnalysisRequest request)
    {
        // Simple heuristic: Try Prefix, if good match return. Try Sequence.
        // In reality, Auto would be more complex.

        var dataList = data.ToList();

        // 1. Check Sequence
        var seqStrategy = _strategies.OfType<SequenceAnalysisStrategy>().FirstOrDefault();
        if (seqStrategy != null)
        {
            var seqReq = new AnalysisRequest { Column = request.Column, AnalysisType = AnalysisType.Sequence };
            var res = seqStrategy.Execute(dataList, seqReq);
            if (res.IsSequential && res.TotalRowsAnalyzed > 0)
            {
                res.AnalysisType = AnalysisType.Auto; // Report as Auto result
                res.Message += " (Auto-detected Sequence)";
                return res;
            }
        }

        // 2. Check Prefix
        var prefixStrategy = _strategies.OfType<PrefixAnalysisStrategy>().FirstOrDefault();
        if (prefixStrategy != null)
        {
            var preReq = new AnalysisRequest { Column = request.Column, AnalysisType = AnalysisType.Prefix };
            var res = prefixStrategy.Execute(dataList, preReq);
            if (!string.IsNullOrEmpty(res.CommonPrefix))
            {
                res.AnalysisType = AnalysisType.Auto;
                res.Message += " (Auto-detected Prefix)";
                return res;
            }
        }

        return new AnalysisResult
        {
            ColumnName = request.Column,
            AnalysisType = AnalysisType.Auto,
            Success = true,
            Message = "No specific pattern detected automatically.",
            TotalRowsAnalyzed = dataList.Count
        };
    }
}
