using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CmdPal.VaultSearchExtension.Indexer.Files;

internal sealed partial class MarkdownFileReader(): IFileReader {

    private static readonly char[] separator = ['\r', '\n'];

    [GeneratedRegex(@"^\s*---\r?\n(.*?)\r?\n---\r?\n", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();

    [GeneratedRegex(@"^\s*title\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"^\s*tags\s*:\s*(.+)$", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex TagsRegex();

    [GeneratedRegex(@"(""[^""]*""|'[^']*'|[^,]+)")]
    private static partial Regex TagRegex2();

    public FileEntry ReadEntry(string filePath, string vaultName, int _maxPreviewBytes = 10240) {
        if(!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = fileInfo.Extension.ToLowerInvariant();

        string title = fileName; // 默认标题为文件名
        List<string> tags = [];
        StringBuilder contentPreview = new();
        bool inFrontMatter = false;
        bool frontMatterParsed = false;
        int contentChars = 0;

        // 逐行读取，惰性加载，可在收集足够内容后退出
        foreach(string line in File.ReadLines(filePath, Encoding.UTF8)) {
            // 检测 front matter 开始/结束
            if(!frontMatterParsed && line.Trim() == "---") {
                if(!inFrontMatter) {
                    inFrontMatter = true;
                    continue;
                } else {
                    inFrontMatter = false;
                    frontMatterParsed = true;
                    continue;
                }
            }

            if(inFrontMatter) {
                var titleMatch = TitleRegex().Match(line);
                if(titleMatch.Success) {
                    title = StripQuotes(titleMatch.Groups[1].Value.Trim());
                    continue;
                }
                var tagsMatch = TagsRegex().Match(line);
                if(tagsMatch.Success) {
                    string tagsValue = tagsMatch.Groups[1].Value.Trim();
                    tags.AddRange(ParseTags(tagsValue));
                }
            } else {
                if(line.TrimStart().StartsWith("#", StringComparison.OrdinalIgnoreCase) && contentChars < _maxPreviewBytes) {
                    string cleanedLine = line.Trim();
                    contentPreview.AppendLine(cleanedLine);
                    contentChars += cleanedLine.Length + Environment.NewLine.Length;
                }

                if(contentChars >= _maxPreviewBytes)
                    break;
            }
        }

        return new FileEntry(
            id: 0,
            filePath: filePath,
            vaultName: vaultName,
            fileName: fileName,
            extension: extension,
            title: title ?? fileName,
            length: fileInfo.Length,
            tags: [.. tags],
            lastModifiedTicks: fileInfo.LastWriteTimeUtc.Ticks,
            contentPreview: contentPreview.ToString().TrimEnd()
        );
    }

    public string ReadContent(string filePath) {
        if(!File.Exists(filePath))
            return string.Empty;

        string content = File.ReadAllText(filePath);

        // 正则匹配开头的 Frontmatter：从文件开始到第一个 "---\n" 之后到第二个 "---\n"
        // 使用非贪婪匹配，并且确保 "---" 单独成行
        var regex = FrontmatterRegex();
        Match match = regex.Match(content);

        if(match.Success && match.Index == 0) {
            // 去掉 Frontmatter
            return content.Substring(match.Length);
        }

        return content;
    }

    private static List<string> ParseTags(string tagsValue) {
        var result = new List<string>();
        if(string.IsNullOrWhiteSpace(tagsValue))
            return result;

        // 1. JSON 数组格式: ["a","b"]
        if(tagsValue.StartsWith("[", StringComparison.OrdinalIgnoreCase) && tagsValue.EndsWith("]", StringComparison.OrdinalIgnoreCase)) {
            string inner = tagsValue.Substring(1, tagsValue.Length - 2);
            var matches = TagRegex2().Matches(inner);
            foreach(Match m in matches) {
                string tag = StripQuotes(m.Value.Trim());
                if(!string.IsNullOrWhiteSpace(tag))
                    result.Add(tag.ToLowerInvariant());
            }
        }
        // 2. YAML 列表格式（以 - 开头）
        else if(tagsValue.StartsWith("-", StringComparison.OrdinalIgnoreCase)) {
            var lines = tagsValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach(var line in lines) {
                string tag = line.TrimStart().TrimStart('-').Trim();
                tag = StripQuotes(tag);
                if(!string.IsNullOrWhiteSpace(tag))
                    result.Add(tag.ToLowerInvariant());
            }
        }
        // 3. 逗号分隔格式: "tag1, tag2"
        else {
            var tags = tagsValue.Split([','], StringSplitOptions.RemoveEmptyEntries);
            foreach(var tag in tags) {
                string cleaned = StripQuotes(tag.Trim());
                if(!string.IsNullOrWhiteSpace(cleaned))
                    result.Add(cleaned.ToLowerInvariant());
            }
        }

        return result;
    }

    private static string StripQuotes(string input) {
        if((input.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && input.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            || (input.StartsWith("'", StringComparison.OrdinalIgnoreCase) && input.EndsWith("'", StringComparison.OrdinalIgnoreCase))) {
            return input[1..^1];
        }
        return input;
    }


}