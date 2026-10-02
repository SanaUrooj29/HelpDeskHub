using HelpDeskHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHub.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Ticket> Tickets { get; set; }

    public DbSet<TicketComment> TicketComments { get; set; }

    public DbSet<TicketActivity> TicketActivities { get; set; }
}