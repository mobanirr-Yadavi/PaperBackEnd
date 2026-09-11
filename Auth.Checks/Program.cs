using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using PaperSite.API.Controllers;
using PaperSite.API.Security;
using PaperSite.Application.Interfaces;
using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Auth;
using PaperSite.Application.Validators.Auth;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
builder.Services.AddSingleton<AuthCookie>();
builder.Services.AddSingleton<IAuthService, FakeAuth>();
builder.Services.AddValidatorsFromAssemblyContaining<SendOtpRequestValidator>();
builder.Services.AddCors(o => o.AddPolicy("FrontendCors", p => p.WithOrigins(BrowserRequestMiddleware.AllowedOrigins)
    .AllowCredentials().AllowAnyHeader().WithMethods("GET", "POST", "PUT", "PATCH", "DELETE").WithExposedHeaders("Retry-After")));
builder.Services.AddRateLimiter(ApiRateLimits.Configure);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, ValidateAudience = false, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FakeAuth.Secret)), ClockSkew = TimeSpan.Zero
    };
    o.Events = new JwtBearerEvents { OnMessageReceived = ctx =>
    {
        if (!ctx.Request.Headers.ContainsKey("Authorization")) ctx.Token = ctx.Request.Cookies[AuthCookie.Name];
        return Task.CompletedTask;
    }};
});
builder.Services.AddAuthorization();
await using var app = builder.Build();
app.UseRouting();
app.UseCors("FrontendCors");
app.UseMiddleware<BrowserRequestMiddleware>();
app.UseMiddleware<OtpValidationMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/profile-check", [Authorize] () => Results.Ok());
await app.StartAsync();
using var client = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(app.Urls.Single()) };
int checks = 0;
void Check(bool result, string label) { if (!result) throw new Exception(label); checks++; Console.WriteLine("PASS " + label); }
async Task<HttpResponseMessage> Post(string action, object body, string? origin = "https://www.kaghaz20.ir")
{
    var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Auth/" + action) { Content = JsonContent.Create(body) };
    if (origin is not null) req.Headers.Add("Origin", origin);
    return await client.SendAsync(req);
}
var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/Auth/Login");
preflight.Headers.Add("Origin", "http://localhost:3000");
preflight.Headers.Add("Access-Control-Request-Method", "POST");
preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
var pf = await client.SendAsync(preflight);
Check(pf.StatusCode == HttpStatusCode.NoContent && pf.Headers.GetValues("Access-Control-Allow-Origin").Single() == "http://localhost:3000"
    && pf.Headers.GetValues("Access-Control-Allow-Credentials").Single() == "true", "CORS preflight");
foreach (var action in new[] { "Login", "Register", "VerifyOtp", "CompleteRegistration" })
{
    var response = await Post(action, new { mobile = "09121111111", code = "123456", email = "a@b.ir", password = "example", registrationToken = "example" });
    var cookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.Single() : "";
    Check(response.IsSuccessStatusCode && cookie.Contains("paper_token=") && cookie.Contains("domain=.kaghaz20.ir")
        && cookie.Contains("secure") && cookie.Contains("httponly") && cookie.Contains("samesite=lax") && cookie.Contains("expires="), action + " cookie");
}
var registration = await Post("VerifyOtp", new { mobile = "09121111111", code = "000000" });
Check(!registration.Headers.Contains("Set-Cookie"), "registration token does not set cookie");
foreach (var bearer in new[] { false, true })
{
    var request = new HttpRequestMessage(HttpMethod.Get, "/profile-check");
    request.Headers.Add(bearer ? "Authorization" : "Cookie", bearer ? "Bearer " + FakeAuth.Token : "paper_token=" + FakeAuth.Token);
    Check((await client.SendAsync(request)).StatusCode == HttpStatusCode.OK, bearer ? "Bearer authentication" : "cookie authentication");
}
var priority = new HttpRequestMessage(HttpMethod.Get, "/profile-check");
priority.Headers.Add("Authorization", "Bearer invalid");
priority.Headers.Add("Cookie", "paper_token=" + FakeAuth.Token);
Check((await client.SendAsync(priority)).StatusCode == HttpStatusCode.Unauthorized, "header takes precedence");
var logout = await Post("Logout", new { });
Check(logout.StatusCode == HttpStatusCode.NoContent && logout.Headers.GetValues("Set-Cookie").Single().Contains("domain=.kaghaz20.ir"), "logout cookie deletion");
Check((await Post("Logout", new {}, "https://evil.test")).StatusCode == HttpStatusCode.Forbidden, "CSRF rejects foreign origin");
Check((await Post("Logout", new {}, null)).StatusCode == HttpStatusCode.Forbidden, "CSRF rejects absent origin");
for (int i = 0; i < 15; i++)
    Check((await Post("SendOtp", new { mobileNo = "x" })).StatusCode == HttpStatusCode.BadRequest, "invalid phone is 400 " + i);
var first = await Post("SendOtp", new { mobileNo = "09121234567" });
Check(first.StatusCode == HttpStatusCode.OK, "invalid phones consumed no quota");
var duplicate = await Post("SendOtp", new { mobileNo = "+98 912-1234567" });
Check(duplicate.StatusCode == HttpStatusCode.TooManyRequests && duplicate.Headers.RetryAfter?.Delta?.TotalSeconds is > 0 and <= 120
    && (await duplicate.Content.ReadFromJsonAsync<BaseResponse<object>>())?.IsSuccess == false, "normalized phone cooldown and JSON retry");
Check((await Post("SendOtp", new { mobileNo = "09121234568" })).StatusCode == HttpStatusCode.OK, "independent phone quota");
var failure = await Post("SendOtp", new { mobileNo = "09120000000" });
Check(failure.StatusCode == HttpStatusCode.BadRequest && (await failure.Content.ReadFromJsonAsync<BaseResponse<object>>())?.IsSuccess == false, "SMS failure stays failure");
var concurrent = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Post("SendOtp", new { mobileNo = "09121234569" })));
Check(concurrent.Count(r => r.StatusCode == HttpStatusCode.OK) == 1 && concurrent.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests) == 3, "concurrent phone requests");
for (int i = 0; i < 6; i++)
    Check((await Post("SendOtp", new { mobileNo = "0912123458" + i })).StatusCode == HttpStatusCode.OK, "eligible IP request " + i);
var capped = await Post("SendOtp", new { mobileNo = "09121234572" });
Check(capped.StatusCode == HttpStatusCode.TooManyRequests && capped.Headers.RetryAfter?.Delta?.TotalSeconds > 120, "IP hourly limit");
using var limiter = new CooldownLimiter(TimeSpan.FromMilliseconds(50));
Check(limiter.AttemptAcquire().IsAcquired && !limiter.AttemptAcquire().IsAcquired, "cooldown acquisition");
await Task.Delay(70);
Check(limiter.AttemptAcquire().IsAcquired && !limiter.AttemptAcquire().IsAcquired, "cooldown renews from latest acceptance");
Console.WriteLine($"Passed {checks} checks. No real database or SMS used.");
await app.StopAsync();

public sealed class FakeAuth : IAuthService
{
    public const string Secret = "test-secret-only-not-for-production-123456789";
    public static string Token => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256)));
    public Task<BaseResponse<AuthResponse>> LoginAsync(LoginRequest r) => Task.FromResult(BaseResponse<AuthResponse>.Success(new() { Token = Token }));
    public Task<BaseResponse<AuthResponse>> RegisterAsync(RegisterRequest r) => Task.FromResult(BaseResponse<AuthResponse>.Success(new() { Token = Token }));
    public Task<BaseResponse<VerifyOtpResponse>> VerifyOtpAsync(string mobile, string code) => Task.FromResult(BaseResponse<VerifyOtpResponse>.Success(
        code == "000000" ? new() { RegistrationToken = "registration-only" } : new() { AccessToken = Token }));
    public Task<BaseResponse<VerifyOtpResponse>> CompleteRegistrationAsync(CompleteRegistrationRequest r) => VerifyOtpAsync("", "123456");
    public Task<BaseResponse<bool>> SendOtpAsync(string mobile) => Task.FromResult(mobile == "09120000000"
        ? BaseResponse<bool>.Failure("ارسال پیامک ناموفق بود") : BaseResponse<bool>.Success(true));
}
