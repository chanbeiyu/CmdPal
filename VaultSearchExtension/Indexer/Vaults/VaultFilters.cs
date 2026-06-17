using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal.VaultSearchExtension.Indexer.Vaults;

internal sealed partial class VaultFilters: Filters {

    private readonly static Filter _filter = new() { Id = "vault.filter._", Name = "All Vaults", Icon = Icons.Search };

    public VaultFilters() {
        CurrentFilterId = "vault.filter._";
    }

    public override IFilterItem[] GetFilters() {
        return [_filter, .. VaultManager.VaultFilters.Values];
    }

}
