using PaperSite.Application.Common.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace PaperSite.Application.Interfaces
{
    public interface ISmsService
    {
        Task<BaseResponse<bool>> SendOtpAsync(string mobile, string code);
    }
}
