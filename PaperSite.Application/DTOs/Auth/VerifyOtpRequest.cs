using System;
using System.Collections.Generic;
using System.Text;

namespace PaperSite.Application.DTOs.Auth
{
    public class VerifyOtpRequest
    {
        public string Mobile { get; set; }
        public string Code { get; set; }
    }
}
