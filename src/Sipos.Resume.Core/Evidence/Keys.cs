using Sipos.Resume.Core.Search;

namespace Sipos.Resume.Core.Evidence;

/// <summary>The key that makes two spellings of a technology the same: its folded tokens joined by spaces.</summary>
public static class Keys
{
    /// <summary>Returns the key of a technology name, such as <c>azure service bus</c> for <c>Azure Service Bus</c>.</summary>
    /// <param name="name">The name as the CV writes it.</param>
    public static string Of(string name) => string.Join(' ', TextNormalizer.Tokens(name));
}
