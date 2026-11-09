using System;
using System.Text;

namespace CyberApp_FIA
{
    /// <summary>
    /// Safe way to put a value inside an XPath query.
    /// XPath 1.0 has no escape character, so a value like  ' or '1'='1  would change the query
    /// if it were pasted between quotes. Lit() always returns a complete, correctly quoted
    /// XPath string literal (including the quotes), so the value can only ever be treated as text.
    ///
    /// Use:   doc.SelectSingleNode($"/events/event[@id={XPathSafe.Lit(eventId)}]")
    /// Not:   doc.SelectSingleNode($"/events/event[@id='{eventId}']")
    /// </summary>
    public static class XPathSafe
    {
        public static string Lit(string value)
        {
            value = value ?? string.Empty;

            // Common case: no apostrophe, so single quotes are safe.
            if (value.IndexOf('\'') < 0) return "'" + value + "'";

            // Has apostrophes but no double quote: wrap in double quotes.
            if (value.IndexOf('"') < 0) return "\"" + value + "\"";

            // Has both kinds of quote: build it with concat(), splitting on each apostrophe.
            var parts = value.Split('\'');
            var sb = new StringBuilder("concat(");
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) sb.Append(", \"'\", ");
                sb.Append('\'').Append(parts[i]).Append('\'');
            }
            return sb.Append(')').ToString();
        }
    }
}
