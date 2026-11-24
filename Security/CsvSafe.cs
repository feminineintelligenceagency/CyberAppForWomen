using System;

namespace CyberApp_FIA
{
    /// <summary>
    /// Safe values for CSV exports.
    /// A cell that starts with =, +, - or @ is run as a formula when the file is opened in Excel or
    /// Sheets (for example =HYPERLINK(...) or =cmd|...). Audit exports contain names and titles that
    /// users typed, so those cells get a leading apostrophe, which makes the spreadsheet treat them as text.
    /// </summary>
    public static class CsvSafe
    {
        public static string Escape(string value)
        {
            if (value == null) return "";

            if (value.Length > 0)
            {
                var first = value[0];
                if (first == '=' || first == '+' || first == '-' || first == '@' || first == '\t' || first == '\r')
                    value = "'" + value;
            }

            if (value.Contains("\"") || value.Contains(",") || value.Contains("\r") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
