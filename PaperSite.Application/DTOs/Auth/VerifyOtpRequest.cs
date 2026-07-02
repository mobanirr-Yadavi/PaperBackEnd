using System;
using System.Collections.Generic;
using System.Text;

namespace PaperSite.Application.DTOs.Auth
{
    public class VerifyOtpRequest
    {
        public string Mobile { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
