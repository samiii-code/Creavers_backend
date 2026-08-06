namespace Creavers.API.Models.Enums
{
    /// <summary>Lifecycle status of a CustomerTask.</summary>
    public enum CustomerTaskStatus
    {
        Pending   = 0,
        Matched   = 1,
        Accepted  = 2,
        Completed = 3,
        Cancelled = 4
    }
}
