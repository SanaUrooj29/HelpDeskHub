using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HelpDeskHub.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Category? Category { get; set; }

    public ICollection<TicketComment> Comments { get; set; }
    = new List<TicketComment>();

    public ICollection<TicketActivity> Activities { get; set; }
    = new List<TicketActivity>();

    // Assigned agent (Identity user id)
    public string? AssignedToId { get; set; }
    public IdentityUser? AssignedTo { get; set; }
}