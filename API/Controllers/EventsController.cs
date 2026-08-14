using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Controllers;

// The EventsController class inherits from BaseApiController and takes an AppDbContext as a constructor parameter. This allows the controller to access the database context for performing CRUD operations on events.
public class EventsController(AppDbContext context) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<Event>>> GetEvent()
    {
        return await context.Events.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEventDetail(string id)
    {
        var evt = await context.Events.FindAsync(id);
        
        if(evt == null) return NotFound();
        
        return evt;
    }
} 
        