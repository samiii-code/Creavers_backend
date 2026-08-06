using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Tasks
{
    /// <summary>Response payload for a customer task.</summary>
    public class TaskResponse
    {
        public Guid              Id           { get; set; }
        public Guid              CustomerId   { get; set; }
        public string            CustomerName { get; set; } = string.Empty;
        public Guid              CategoryId   { get; set; }
        public string            CategoryName { get; set; } = string.Empty;
        public string            Title        { get; set; } = string.Empty;
        public string            Description  { get; set; } = string.Empty;
        public string            Address      { get; set; } = string.Empty;
        public string            SubCity      { get; set; } = string.Empty;
        public string            Woreda       { get; set; } = string.Empty;
        public string?           Landmark     { get; set; }
        public decimal           Budget       { get; set; }
        public DateTime          PreferredDate { get; set; }
        public CustomerTaskStatus Status      { get; set; }
        public string?           ImagePath    { get; set; }
        public DateTime          CreatedAt    { get; set; }
        public DateTime?         UpdatedAt    { get; set; }
    }
}
