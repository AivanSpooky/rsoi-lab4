namespace Gateway.Resilience;

public static class ErrorClassifier
{
    public static bool IsTransient(Exception exception)
    {
        return exception switch
        {
            HttpRequestException http => http.StatusCode is null || (int)http.StatusCode >= 500,
            _ => true
        };
    }
}
