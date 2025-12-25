using Microsoft.AspNetCore.Mvc;

namespace Travelette.Relay.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SettingsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetSettings()
    {
        return Ok(new
        {
            currency = _configuration["AppSettings:Currency"] ?? "USD",
            currencySymbol = _configuration["AppSettings:CurrencySymbol"] ?? "$"
        });
    }
}

