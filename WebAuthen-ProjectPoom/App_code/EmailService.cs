using System.Net;
using System.Net.Mail;

namespace WebAuthen.App_code;

public class EmailService
{
  private static readonly SmtpClient _smtp = new()
  {
    Host = "43.241.56.25",
    Credentials = new NetworkCredential("workiwise@bsv-th.com", "Bsv@1234"),
    Port = 587,
    EnableSsl = false,
    UseDefaultCredentials = false,
    DeliveryMethod = SmtpDeliveryMethod.Network,
  };

  public async Task SendOtpEmailAsync(string toEmail, string reference, string otp)
  {

    var Body = $@"
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset='utf-8'>
          <style>
            body {{
              font-family: 'Segoe UI', Tahoma, sans-serif;
              background-color: #f5f5f5;
              margin: 0;
              padding: 20px;
              color: #333;
            }}
            .email-container {{
              max-width: 600px;
              margin: auto;
              background-color: #ffffff;
              padding: 24px;
              border-radius: 8px;
              box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            }}
            .header {{
              text-align: center;
              margin-bottom: 20px;
            }}
            .header h1 {{
              margin: 0;
              font-size: 28px;
              color: #2a9d8f;
            }}
            .content p {{
              line-height: 1.6;
              margin: 12px 0;
            }}
            .otp-box {{
              background-color: #e0f7fa;
              padding: 16px;
              text-align: center;
              border-radius: 6px;
              margin: 20px 0;
            }}
            .otp-box .otp-code {{
              font-size: 32px;
              font-weight: bold;
              letter-spacing: 4px;
              color: #00796b;
              margin: 8px 0;
            }}
            .footer {{
              margin-top: 30px;
              font-size: 14px;
              color: #555;
              text-align: center;
            }}
          </style>
        </head>
        <body>
          <div class='email-container'>
            <div class='header'>
              <h1>Workiwise</h1>
            </div>
            <div class='content'>
              <p>สวัสดีครับ,</p>
              <p>Reference ID: <strong>{reference}</strong></p>
              <div class='otp-box'>
                <p>รหัสยืนยันของคุณ (OTP Code)</p>
                <p class='otp-code'>{otp}</p>
              </div>
              <p>กรุณากรอกรหัสนี้ภายใน <strong>5 นาที</strong> เพื่อยืนยันตัวตนของคุณ</p>
              <p>หากคุณไม่ได้ขอรหัสนี้ กรุณาเพิกเฉยหรือแจ้งทีมสนับสนุนทันที</p>
            </div>
            <div class='footer'>
              <p>ขอบคุณครับ,</p>
              <p>ทีมงาน Workiwise</p>
            </div>
          </div>
        </body>
        </html>
        ";

    var mail = new MailMessage("workiwise@bsv-th.com", toEmail)
    {
      Subject = "รหัส OTP สำหรับรีเซ็ตรหัสผ่าน",
      Body = Body,
      IsBodyHtml = true
    };
    await _smtp.SendMailAsync(mail);
  }
}
