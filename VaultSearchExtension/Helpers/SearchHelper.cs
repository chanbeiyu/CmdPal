using CmdPal.VaultSearchExtension.Indexer;
using CmdPal.VaultSearchExtension.Indexer.Search;
using CmdPal.VaultSearchExtension.Pages;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;
using System.Linq;

namespace CmdPal.VaultSearchExtension.Helpers;

internal static class SearchHelper {
    public static IReadOnlyList<ListItem> Search(string query, int skip = 0, int take = 10, string? vaultName = null) {
        IReadOnlyList<SearchResult> results = FileCacheManager.Instance.Search(query, skip, take, vaultName);
        if(results.Count == 0) {
            return [];
        }
        var listItems = results
            .Select(result => new FileListItem(result))
            .ToList();
        return listItems;
    }

}
