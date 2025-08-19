

namespace Application
{
    public static class StringExtensions
    {

        public static string ShortenText(this string input, int maxLength = 30)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            {
                return input;
            }

            return string.Concat(input.AsSpan(0, maxLength - 3), "...");
        }




        public static string Persianize(this string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return null;
            }

            //return Lname.Replace((char)1740, (char)1610).Replace((char)1705, (char)1603).Trim();


            //return Lname.Replace((char)1740, (char)1610).Replace((char)1705, (char)1603).Trim();
            return text.Replace("\u064a", "\u06cc").Replace("\u0643", "\u06a9").Trim();

        }

    }
}
