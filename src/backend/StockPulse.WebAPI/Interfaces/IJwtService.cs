namespace StockPulse.WebAPI.Interfaces;

public interface IJwtService
{
    string GenerateToken(string email);
}
