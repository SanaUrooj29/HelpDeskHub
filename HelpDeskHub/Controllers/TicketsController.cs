using HelpDeskHub.Data;
using HelpDeskHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHub.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TicketsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Tickets
    public async Task<IActionResult> Index()
    {
        var tickets = await _context.Tickets
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(tickets);
    }

    // Support agents can use this to view their assigned and unassigned work
    [Authorize(Roles = "SupportAgent")]
    public async Task<IActionResult> AgentWorklist()
    {
        var tickets = await _context.Tickets
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View("Index", tickets);
    }

    // Administrators can view all tickets via this action
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> All()
    {
        var tickets = await _context.Tickets
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View("Index", tickets);
    }

    // GET: /Tickets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Comments)
            .Include(t => t.Activities)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    // GET: /Tickets/Create
    [Authorize(Roles = "Employee,Administrator")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View();
    }

    // POST: /Tickets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Employee,Administrator")]
    public async Task<IActionResult> Create(Ticket ticket)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(error =>
                    $"{x.Key}: {error.ErrorMessage}"))
                .ToList();

            ViewBag.Errors = errors;

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(ticket);
        }

        ticket.Status = TicketStatus.Open;
        ticket.Priority = TicketPriority.Medium;
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.Tickets.Add(ticket);

        // Save first so the Ticket gets its database Id.
        await _context.SaveChangesAsync();

        // Create initial activity/history entry.
        var activity = new TicketActivity
        {
            TicketId = ticket.Id,
            ActivityType = "TicketCreated",
            Description = "Ticket was created.",
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketActivities.Add(activity);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Tickets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(ticket);
    }

    // POST: /Tickets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Ticket ticket)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(ticket);
        }

        var existingTicket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == id);

        if (existingTicket == null)
        {
            return NotFound();
        }

        // Store the old values before changing them.
        var oldStatus = existingTicket.Status;
        var oldPriority = existingTicket.Priority;

        // Update ticket fields.
        existingTicket.Title = ticket.Title;
        existingTicket.Description = ticket.Description;
        existingTicket.CategoryId = ticket.CategoryId;
        existingTicket.Priority = ticket.Priority;
        existingTicket.Status = ticket.Status;
        existingTicket.UpdatedAt = DateTime.UtcNow;

        // Handle resolved date.
        if (ticket.Status == TicketStatus.Resolved)
        {
            existingTicket.ResolvedAt ??= DateTime.UtcNow;
        }
        else
        {
            existingTicket.ResolvedAt = null;
        }

        // Record status change.
        if (oldStatus != existingTicket.Status)
        {
            _context.TicketActivities.Add(new TicketActivity
            {
                TicketId = existingTicket.Id,
                ActivityType = "StatusChanged",
                Description =
                    $"Status changed from {oldStatus} to {existingTicket.Status}.",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Record priority change.
        if (oldPriority != existingTicket.Priority)
        {
            _context.TicketActivities.Add(new TicketActivity
            {
                TicketId = existingTicket.Id,
                ActivityType = "PriorityChanged",
                Description =
                    $"Priority changed from {oldPriority} to {existingTicket.Priority}.",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Tickets/AddComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        int ticketId,
        string commentText)
    {
        if (string.IsNullOrWhiteSpace(commentText))
        {
            return RedirectToAction(
                nameof(Details),
                new { id = ticketId });
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
        {
            return NotFound();
        }

        var comment = new TicketComment
        {
            TicketId = ticketId,
            CommentText = commentText.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketComments.Add(comment);

        ticket.UpdatedAt = DateTime.UtcNow;

        // Record comment activity.
        var activity = new TicketActivity
        {
            TicketId = ticketId,
            ActivityType = "CommentAdded",
            Description = "A comment was added to the ticket.",
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketActivities.Add(activity);

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id = ticketId });
    }
}