using DraftService.Models;
using DraftService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DraftService.Controllers;

[ApiController]
[Route("api/journalists")]
public class JournalistsController : ControllerBase
{
    private readonly IJournalistRepository _repository;

    public JournalistsController(IJournalistRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Journalist>>> GetAll()
    {
        return Ok(await _repository.GetAllAsync());
    }
}
