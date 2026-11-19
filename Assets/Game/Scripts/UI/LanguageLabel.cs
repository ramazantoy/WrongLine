namespace WrongLine.UI
{
    /// <summary>How a language is named on the settings screen: its own name, without the region ("Türkçe (Türkiye)" shows as "Türkçe").</summary>
    public static class LanguageLabel
    {
        public static string Format(string nativeName)
        {
            if (string.IsNullOrWhiteSpace(nativeName))
            {
                return string.Empty;
            }

            int region = nativeName.IndexOf(" (", System.StringComparison.Ordinal);
            return (region > 0 ? nativeName.Substring(0, region) : nativeName).Trim();
        }
    }
}
