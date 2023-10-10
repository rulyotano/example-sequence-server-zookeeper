using System.Net;
using Microsoft.AspNetCore.Mvc;
using SequenceNode.Application;

namespace SequenceNode.Controllers;

[ApiController]
[Route("[controller]")]
public class SequenceController : ControllerBase
{
    private readonly IDistributedConfiguration _distributedConfiguration;

    public SequenceController(IDistributedConfiguration distributedConfiguration)
    {
        _distributedConfiguration = distributedConfiguration;
    }

    [HttpGet]
    [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetSequenceNumber(CancellationToken cancellationToken = default)
    {
        return Ok(await _distributedConfiguration.GetSequenceNumberAsync(cancellationToken));
    }
}
