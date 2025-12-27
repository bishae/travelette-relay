using Microsoft.Extensions.Configuration;
using Travelette.Relay.Application.Services;

namespace Travelette.Relay.Infrastructure.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetCurrencySymbol()
    {
        return _configuration["AppSettings:CurrencySymbol"] ?? "$";
    }

    public string GetCurrency()
    {
        return _configuration["AppSettings:Currency"] ?? "USD";
    }
}

