using System.ComponentModel.DataAnnotations;

namespace HelpDeskHub.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    [Required]
    [StringLength(2000)]
    public string CommentText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;
}