using PaperSite.Domain.Entities;

namespace PaperSite.Application.Interfaces;

public interface IJWtService
{
    string GenerateToken(User user);
}
