using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CmdPal.VaultSearchExtension.Indexer.Files;

internal static class FileReaderFactory
{
    private static readonly Dictionary<string, IFileReader> Readers = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".md", new MarkdownFileReader() },
            { ".markdown", new MarkdownFileReader() },
            { ".txt", new TextFileReader() },
            { ".text", new TextFileReader() }
        };

    // 使用 HashSet 提高查找性能 O(1) vs O(n)
    private static readonly HashSet<string> _includeExts = new(StringComparer.OrdinalIgnoreCase) { 
        ".md", ".text", ".markdown", ".txt" 
    };

    private static readonly HashSet<string> _excludeFolders = new(StringComparer.OrdinalIgnoreCase) { 
        ".git", ".obsidian", "node_modules", ".trash", ".github", ".cursor", ".codex", ".claude", ".opencode"
    };

    public static IFileReader GetReader(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        if (Readers.TryGetValue(ext, out var reader))
            return reader;
        return new TextFileReader();
    }

    public static void Register(string extension, IFileReader reader)
    {
        Readers[extension] = reader;
    }

    public static List<string> FilesRecursive(string rootDir)
    {
        return FilesRecursive(rootDir, _excludeFolders, _includeExts);
    }

    public static List<string> FilesRecursive(string rootDir, HashSet<string> excludeFolders, HashSet<string> includeExts)
    {
        var result = new List<string>();
        var dirStack = new Stack<string>();
        dirStack.Push(rootDir);

        var extLookup = new HashSet<string>(
            includeExts.Select(ext => ext.StartsWith('.') ? ext : "." + ext),
            StringComparer.OrdinalIgnoreCase
        );

        while (dirStack.Count > 0)
        {
            string currentDir = dirStack.Pop();

            if (!Directory.Exists(currentDir)) continue;

            string folderName = Path.GetFileName(currentDir);
            if (excludeFolders.Contains(folderName)) continue;

            try
            {
                foreach (var file in Directory.EnumerateFiles(currentDir))
                {
                    if (extLookup.Contains(Path.GetExtension(file)))
                        result.Add(file);
                }

                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    dirStack.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
        }

        return result;
    }

}