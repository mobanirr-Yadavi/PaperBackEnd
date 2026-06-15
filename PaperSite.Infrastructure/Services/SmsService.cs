using Microsoft.Extensions.Options;
using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Auth;
using PaperSite.Application.DTOs.Product;
using PaperSite.Application.Interfaces;
using PaperSite.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PaperSite.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly SmsSettings _settings;
        private readonly HttpClient _httpClient;

        public SmsService(IOptions<SmsSettings> options, HttpClient httpClient)
        {
            _settings = options.Value;
            _httpClient = httpClient;
        }

        public async Task<BaseResponse<bool>> SendOtpAsync(string mobile, string code)
        {
            mobile = mobile.Substring(1);
            var request = new
            {
                mobile,
                templateId = int.Parse(_settings.TemplateId),
                parameters = new[]
                {
                new { name = "Code", value = code }
            }
            };
            
            var json = JsonSerializer.Serialize(request);

            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.sms.ir/v1/send/verify"
            );

            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var x = response;
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
            if (!response.IsSuccessStatusCode)
                return BaseResponse<bool>.Failure("ایمیل یا گذرواژه نامعتبر است");
            return BaseResponse<bool>.Success(true, "پیامک ارسال شد");

        }

    }
}

