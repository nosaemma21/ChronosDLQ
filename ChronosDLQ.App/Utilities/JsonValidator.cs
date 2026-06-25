namespace ChronosDLQ.App.Utilities;

public static class JsonValidator
{
    public static bool IsValidJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(input);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
