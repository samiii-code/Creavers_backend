using Creavers.API.Models.Enums;

namespace Creavers.API.Models
{
    /// <summary>A service task created by a customer.</summary>
    public class CustomerTask : BaseEntity
    {
        public Guid               CustomerId    { get; set; }
        public Guid               CategoryId    { get; set; }
        public string             Title         { get; set; } = string.Empty;
        public string             Description   { get; set; } = string.Empty;

        // ── Address fields ──────────────────────────────────────────────────────
        public string             Address       { get; set; } = string.Empty;
        public string             SubCity       { get; set; } = string.Empty;   // e.g. Bole, Yeka
        public string             Woreda        { get; set; } = string.Empty;
        public string?            Landmark      { get; set; }
        public double?            Latitude      { get; set; }
        public double?            Longitude     { get; set; }

        // ── Task details ────────────────────────────────────────────────────────
        public decimal            Budget        { get; set; }
        public DateTime           PreferredDate { get; set; }
        public CustomerTaskStatus Status        { get; set; } = CustomerTaskStatus.Pending;
        public string?            ImagePath     { get; set; }

        // Navigation
        public ApplicationUser    Customer      { get; set; } = null!;
        public Category           Category      { get; set; } = null!;
    }
}
