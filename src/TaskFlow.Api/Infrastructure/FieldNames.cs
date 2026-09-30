using System.Text;

namespace TaskFlow.Api.Infrastructure;

/// <summary>Human-readable field names for error messages: "sortBy" / "SortBy" → "Sort by".</summary>
public static class FieldNames
{
    public static string Display(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "Value";
        }

        var text = new StringBuilder(field.Length + 4);
        for (var i = 0; i < field.Length; i++)
        {
            var c = field[i];
            if (i == 0)
            {
                text.Append(char.ToUpperInvariant(c));
            }
            else if (char.IsUpper(c))
            {
                text.Append(' ').Append(char.ToLowerInvariant(c));
            }
            else
            {
                text.Append(c);
            }
        }

        return text.ToString();
    }
}
