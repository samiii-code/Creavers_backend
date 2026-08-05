namespace Creavers.API.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime UtcNow => DateTime.UtcNow;

        public static long ToUnixTimestamp(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }
    }
}
