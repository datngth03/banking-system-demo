namespace BankingSystem.Application.Interfaces;

public interface IUserService
{
    Task<bool> UserExistsAsync(Guid userId);
    Task<bool> EmailExistsAsync(string email);
    Task<dynamic?> GetUserByIdAsync(Guid userId);
    Task<dynamic?> GetUserByEmailAsync(string email);
}
