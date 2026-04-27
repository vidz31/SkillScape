using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using SkillScape.Infrastructure.Data;
using SkillScape.Domain.Entities;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DebugController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("test-save")]
    public async Task<IActionResult> TestSave()
    {
        try
        {
            var user = _db.Users.FirstOrDefault();
            if (user == null) return BadRequest("No user");

            var result = new QuizResult
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                RecommendedDomainId = "fullstack",
                ScoresJson = "{}",
                RecommendationReason = "Test",
                CompletedAt = DateTime.UtcNow
            };

            _db.QuizResults.Add(result);
            await _db.SaveChangesAsync();

            return Ok("Saved successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                Message = ex.Message, 
                InnerMessage = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
