using Microsoft.AspNetCore.Mvc;
using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Auth;
using PaperSite.Application.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
namespace PaperSite.API.Controllers;

[EnableRateLimiting("AuthLimiter")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ISmsService _smsService;
    public AuthController(IAuthService authService,ISmsService smsService)
    {
        _authService = authService;
        _smsService = smsService;
    }

    /// <summary>
    /// ثبت‌نام کاربر جدید در سیستم
    /// </summary>
    /// <param name="request">اطلاعات مورد نیاز برای ایجاد حساب کاربری</param>
    /// <returns>نتیجه عملیات ثبت‌نام کاربر</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// ورود کاربر به سیستم
    /// </summary>
    /// <param name="request">اطلاعات ورود شامل نام کاربری یا ایمیل و رمز عبور</param>
    /// <returns>توکن احراز هویت و اطلاعات کاربر در صورت موفقیت</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// ارسال رمز یکبار مصرف و فراخوانی سرویس پنل اس ام اس
    /// </summary>
    /// <param name="request">اطلاعات ورود با رمز یگبارمصرف </param>
    /// <returns>ارسال پیامک</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp(SendOtpRequest request)
    {
        var result = await _authService.SendOtpAsync(request.mobileNo);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// ورود کاربر به سیستم با رمز یکبار مصرف
    /// </summary>
    /// <param name="request"> ورود با رمز یگبارمصرف و تایید آن </param>
    /// <returns>ارسال پیامک</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var result = await _authService.VerifyOtpAsync(request.Mobile, request.Code);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
