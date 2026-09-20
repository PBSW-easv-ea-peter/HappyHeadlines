namespace HappyHeadlinesPages.web;

public static class Greetings
{
    public static string TimeOfDay() => DateTime.Now.Hour switch
    {
        < 12 => "morning",
        < 18 => "afternoon",
        _ => "evening"
    };
}
