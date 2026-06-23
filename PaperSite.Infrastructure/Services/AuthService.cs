using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Auth;
using PaperSite.Application.Interfaces;
using System.Security.Cryptography;
using PaperSite.Domain.Entities;

namespace PaperSite.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<OtpCode> _otpRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly PasswordHasher<OtpCode> _otpHasher = new();
    private readonly IJWtService _jwtService;
    private readonly ISmsService _smsService;

    public AuthService(IUnitOfWork unitOfWork, IRepository<User> userRepository, IRepository<Role> roleRepository, IJWtService jwtService,ISmsService smsService,IRepository<OtpCode> otpRepository)
    {
        _otpRepository = otpRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtService = jwtService;
        _smsService = smsService;
    }

    public async Task<BaseResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.Query()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
        {
            return BaseResponse<AuthResponse>.Failure("ایمیل یا گذرواژه نامعتبر است");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return BaseResponse<AuthResponse>.Failure("ایمیل یا گذرواژه نامعتبر است");
        }

        var token = _jwtService.GenerateToken(user);
        return BaseResponse<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            Role = user.Role.Name,
            Token = token
        }, "ورود با موفقیت انجام شد");
    }

    public async Task<BaseResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingEmail = await _userRepository.FirstOrDefaultAsync(x => x.Email == request.Email);
        if (existingEmail != null)
        {
            return BaseResponse<AuthResponse>.Failure("ایمیل قبلاً ثبت شده است");
        }

        var existingPhone = await _userRepository.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber);
        if (existingPhone != null)
        {
            return BaseResponse<AuthResponse>.Failure("شماره تلفن قبلاً ثبت شده است");
        }

        var customerRole = await _roleRepository.FirstOrDefaultAsync(x => x.Name == Role.Customer);
        if (customerRole == null)
        {
            return BaseResponse<AuthResponse>.Failure("نقش مشتری یافت نشد");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            RoleId = customerRole.Id,
            Role = customerRole
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return BaseResponse<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            Role = customerRole.Name,
            Token = token
        }, "ثبت‌نام با موفقیت انجام شد");
    }
    public async Task<BaseResponse<bool>> SendOtpAsync(string mobile)
    {
        var user = await _userRepository.FirstOrDefaultAsync(x => x.PhoneNumber == mobile);
        if (user == null)
            return BaseResponse<bool>.Failure("کاربر یافت نشد");
        var now = DateTime.Now;

        var otpCountInLastMinute = await _otpRepository.Query()
            .CountAsync(x =>
                x.UserId == user.Id &&
                x.CreatedAt >= now.AddMinutes(-1));

        if (otpCountInLastMinute >= 3)
        {
            return BaseResponse<bool>.Failure("برای این شماره بیش از حد کد ارسال شده است. لطفاً یک دقیقه بعد دوباره تلاش کنید.");
        }
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.Now.AddMinutes(2)
        };
        otp.CodeHash = _otpHasher.HashPassword(otp, code);
        await _otpRepository.AddAsync(otp);
        await _unitOfWork.SaveChangesAsync();

        var smsResult = await _smsService.SendOtpAsync(mobile, code);
        if (!smsResult.IsSuccess)
            return BaseResponse<bool>.Failure("ارسال پیامک ناموفق بود");

        return BaseResponse<bool>.Success(true, "کد ارسال شد");
    }

    public async Task<BaseResponse<string>> VerifyOtpAsync(string mobile, string code)
    {
        var user = await _userRepository.Query()
      .Include(x => x.Role)
      .FirstOrDefaultAsync(x => x.PhoneNumber == mobile);

        if (user == null)
        {
            return BaseResponse<string>.Failure("کاربر یافت نشد");
        }

        var now = DateTime.UtcNow;

        var otpCodes = await _otpRepository.Query()
            .Where(x =>
                x.UserId == user.Id &&
                x.UsedAt == null &&
                x.ExpiresAt >= now)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        var otp = otpCodes.FirstOrDefault(x =>
        {
            var result = _otpHasher.VerifyHashedPassword(x, x.CodeHash, code);

            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        });

        if (otp == null)
        {
            return BaseResponse<string>.Failure("کد اشتباه یا منقضی شده است");
        }

        otp.UsedAt = now;
        otp.UpdatedAt = now;

        _otpRepository.Update(otp);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return BaseResponse<string>.Success(token, "ورود موفق");
    }
}
