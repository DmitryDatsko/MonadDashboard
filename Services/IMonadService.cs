using MonadDashboard.Models.DTO;

namespace MonadDashboard.Services;

public interface IMonadService
{
    Task<Overview> GetOverview();
}