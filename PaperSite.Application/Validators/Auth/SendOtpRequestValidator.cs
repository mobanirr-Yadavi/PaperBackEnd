using FluentValidation;
using PaperSite.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace PaperSite.Application.Validators.Auth
{
    public class SendOtpRequestValidator:AbstractValidator<SendOtpRequest>
    {
        public SendOtpRequestValidator()
        {
            RuleFor(x => x.mobileNo)
                .NotEmpty().WithMessage("شماره موبایل الزامی است.")
                .Matches(@"^09\d{9}$").WithMessage("شماره موبایل معتبر نیست.");
        }
    }
}
