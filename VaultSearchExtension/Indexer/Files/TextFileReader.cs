using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CmdPal.VaultSearchExtension.Indexer.Files;

internal sealed partial class TextFileReader(): IFileReader {
    [GeneratedRegex(@"^#(\w[\w-]*)", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    public FileEntry ReadEntry(string filePath, string vaultName, int _maxPreviewBytes = 10240) {
        if(!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = fileInfo.Extension.ToLowerInvariant();
        string title = fileName;

        List<string> tags = [];
        string contentPreview = ReadFilePreviewAndTags(filePath, _maxPreviewBytes, tags, Encoding.UTF8);

        return new FileEntry(
            id: 0,
            filePath: filePath,
            vaultName: vaultName,
            fileName: fileName,
            extension: extension,
            length: fileInfo.Length,
            title: title,
            tags: [.. tags],
            lastModifiedTicks: fileInfo.LastWriteTimeUtc.Ticks,
            contentPreview: contentPreview
        );
    }

    public string ReadContent(string filePath) {
        if(!File.Exists(filePath))
            return string.Empty;
        return File.ReadAllText(filePath);
    }

    private static string ReadFilePreviewAndTags(string filePath, int maxBytes, List<string> tagsCollector, Encoding encoding) {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // 读取指定字节数（额外多读几字节避免截断多字节字符）
        int bytesToRead = (int)Math.Min(maxBytes, fs.Length);
        byte[] buffer = new byte[bytesToRead + encoding.GetMaxByteCount(1) - 1];
        int bytesRead = fs.Read(buffer, 0, buffer.Length);

        // 解码到字符串，保证不截断字符
        var decoder = encoding.GetDecoder();
        int charCount = decoder.GetCharCount(buffer, 0, bytesRead);
        char[] chars = new char[charCount];
        decoder.GetChars(buffer, 0, bytesRead, chars, 0);

        string content = new(chars);

        // 提取标签：#xxx 形式（以 # 开头且后面紧跟非空白字符）
        var tagRegex = TagRegex();
        foreach(Match match in tagRegex.Matches(content)) {
            string tag = match.Groups[1].Value.ToLowerInvariant();
            if(!tagsCollector.Contains(tag))
                tagsCollector.Add(tag);
        }

        // 返回预览内容（限制在要求的字节长度内）
        // 注意：content 已包含不超过 maxBytes 的完整字符，但字符串长度可能大于 maxBytes
        return content;
    }

}
