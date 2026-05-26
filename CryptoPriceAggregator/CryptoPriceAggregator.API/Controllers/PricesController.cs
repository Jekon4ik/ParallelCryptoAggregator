using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPriceAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PricesController : ControllerBase
{
    private readonly IParallelPriceFetchService   _parallelService;
    private readonly ISequentialPriceFetchService _sequentialService;

    public PricesController(
        IParallelPriceFetchService   parallelService,
        ISequentialPriceFetchService sequentialService)
    {
        _parallelService   = parallelService;
        _sequentialService = sequentialService;
    }

    [HttpGet("parallel/{symbol}")]
    [ProducesResponseType(typeof(AggregatedPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetParallel(
        [FromRoute] string symbol,
        CancellationToken  cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        var result = await _parallelService.FetchAllAsync(symbol.ToUpperInvariant(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sequential/{symbol}")]
    [ProducesResponseType(typeof(AggregatedPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSequential(
        [FromRoute] string symbol,
        CancellationToken  cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        var result = await _sequentialService.FetchAllAsync(symbol.ToUpperInvariant(), cancellationToken);
        return Ok(result);
    }
}
