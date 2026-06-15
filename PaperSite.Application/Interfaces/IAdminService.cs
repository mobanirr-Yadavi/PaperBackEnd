using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Admin;

namespace PaperSite.Application.Interfaces;

public interface IAdminService
{
    Task<BaseResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<BaseResponse<UserDto>> GetUserDetailsAsync(Guid userId);
    Task<BaseResponse<DashboardStatisticsDto>> GetDashboardStatisticsAsync();
}
