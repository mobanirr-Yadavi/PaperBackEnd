using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Auth;

namespace PaperSite.Application.Interfaces;

public interface IAuthService
{
    Task<BaseResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<BaseResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<BaseResponse<bool>> SendOtpAsync(string mobile);
    Task<BaseResponse<string>> VerifyOtpAsync(string mobile, string code);
}
