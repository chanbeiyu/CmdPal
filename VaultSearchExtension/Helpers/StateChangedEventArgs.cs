namespace CmdPal.VaultSearchExtension.Helpers;

internal sealed class StateChangedEventArgs<T>(T oldValue, T newValue)
{
    private readonly T oldValue = oldValue;
    private readonly T newValue = newValue;
}