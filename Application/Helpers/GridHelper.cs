using Application.Models.Common;

namespace Application.Helpers
{
    public class GridHelper
    {
        public static string BuildOrderByClause(Sorting[] sorting, string defaultColumn = "Id", string defaultDirection = "DESC")
        {
            if (sorting == null || sorting.Count() == 0)
                return $"{defaultColumn} {defaultDirection}";

            var clauses = new List<string>();

            foreach (var sort in sorting)
            {
                // ✅ prevent injection: allow only letters, numbers, underscore, dot, brackets
                var col = sort.Id;
                if (!System.Text.RegularExpressions.Regex.IsMatch(col, @"^[\w\.\[\]]+$"))
                    throw new ArgumentException($"Invalid sort column: {col}");

                var dir = sort.Desc ? "DESC" : "ASC";
                clauses.Add($"{col} {dir}");
            }

            return string.Join(", ", clauses);
        }
    }
}
