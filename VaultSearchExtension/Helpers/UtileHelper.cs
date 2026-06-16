using System;
using System.Security.Cryptography;
using System.Text;

namespace CmdPal.VaultSearchExtension.Helpers;

internal sealed class UtileHelper
{
    public static string ComputeMD5(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

}
