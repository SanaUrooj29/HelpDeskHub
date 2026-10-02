using System.ComponentModel.DataAnnotations;

namespace HelpDeskHub.Models;

public class TicketActivity
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    [Required]
    [StringLength(500)]
    public string ActivityType { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;
}