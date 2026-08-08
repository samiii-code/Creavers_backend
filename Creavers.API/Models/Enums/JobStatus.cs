namespace Creavers.API.Models.Enums
{
    public enum JobStatus
    {
        Accepted         = 0,
        EnRoute          = 1,
        Arrived          = 2,
        StartPendingOTP  = 3,
        InProgress       = 4,
        Completed        = 5,
        Cancelled        = 6
    }
}
