# راه‌اندازی درگاه ملت

متغیرهای محرمانه زیر را روی سرور تنظیم کنید و داخل Git قرار ندهید:

```text
MellatPayment__TerminalId
MellatPayment__UserName
MellatPayment__UserPassword
```

Callback واقعی:

```text
https://www.kaghaz20.ir/api/v1/payment/mellatcallback
```

این Callback و IP عمومی سرور باید نزد به‌پرداخت ثبت شده باشند.

Migration را اعمال کنید:

```text
dotnet ef database update --project PaperSite.Infrastructure --startup-project PaperSite.API
```

فرانت‌اند با JWT کاربر درخواست زیر را ارسال می‌کند:

```http
POST /api/v1/payment/request
Content-Type: application/json
Authorization: Bearer {token}

{ "orderId": "شناسه سفارش" }
```

پاسخ شامل `paymentUrl` و `refId` است. `RefId` باید با فرم `POST` به `paymentUrl` ارسال شود.
