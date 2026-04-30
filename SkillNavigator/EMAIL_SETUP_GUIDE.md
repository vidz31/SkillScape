# Email Service Configuration Guide

## Overview
The forgot password feature now includes a complete email service integration using SMTP.

## Setup Instructions

### 1. Configure Email Settings

Edit `backend/SkillScape/SkillScape.API/appsettings.json`:

```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderName": "SkillScape Platform",
  "SenderEmail": "your-email@gmail.com",
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "EnableSsl": true,
  "FrontendUrl": "http://localhost:5173"
}
```

### 2. Gmail Setup (Recommended for Testing)

If using Gmail:

1. **Enable 2-Factor Authentication** on your Google account
2. **Generate an App Password**:
   - Go to https://myaccount.google.com/apppasswords
   - Select "Mail" and your device
   - Copy the generated 16-character password
   - Use this password in the `Password` field (NOT your regular Gmail password)

### 3. Alternative Email Providers

#### Outlook/Hotmail
```json
"SmtpServer": "smtp-mail.outlook.com",
"SmtpPort": 587,
```

#### Yahoo Mail
```json
"SmtpServer": "smtp.mail.yahoo.com",
"SmtpPort": 587,
```

#### SendGrid
```json
"SmtpServer": "smtp.sendgrid.net",
"SmtpPort": 587,
"Username": "apikey",
"Password": "your-sendgrid-api-key",
```

### 4. Testing the Feature

1. **Start the backend server**:
   ```bash
   cd backend/SkillScape/SkillScape.API
   dotnet run
   ```

2. **Start the frontend**:
   ```bash
   cd frontend
   npm run dev
   ```

3. **Test forgot password**:
   - Navigate to login page
   - Click "Forgot password?"
   - Enter your email address
   - Check your email for the reset link
   - Click the link or copy/paste the URL
   - Enter your new password

### 5. Email Templates

The service includes professional HTML email templates:

- **Password Reset Email**: Includes branded header, reset button, and security warning
- **Welcome Email**: Sent on registration (optional)

To customize templates, edit `backend/SkillScape/SkillScape.Infrastructure/Services/EmailService.cs`

## Security Notes

- Reset tokens expire after 1 hour
- Tokens are stored securely in the database
- Email addresses are not revealed in responses (prevents user enumeration)
- SSL/TLS encryption is enabled by default

## Troubleshooting

### Email Not Sending

1. **Check backend logs** for error messages
2. **Verify credentials** in appsettings.json
3. **Check spam folder** for received emails
4. **For Gmail**: Ensure App Password is used (not regular password)
5. **Firewall/Antivirus**: May block SMTP ports (587, 465)

### Common Errors

- **Authentication failed**: Wrong username/password
- **Connection timeout**: Firewall blocking SMTP port
- **SSL error**: Try changing `EnableSsl` to false (not recommended for production)

## Production Recommendations

1. **Use environment variables** for sensitive data:
   ```bash
   export EmailSettings__Password="your-app-password"
   ```

2. **Use a dedicated email service**:
   - SendGrid (recommended)
   - Mailgun
   - AWS SES
   - Postmark

3. **Store secrets in Azure Key Vault** or AWS Secrets Manager

4. **Monitor email delivery** with service dashboards

5. **Set up email templates** in your email service provider

## Email Service Features

- ✅ Password reset emails
- ✅ HTML email templates
- ✅ Professional styling
- ✅ Responsive design
- ✅ Error handling and logging
- ✅ Token-based password reset
- ✅ Welcome emails (can be enabled)

## Next Steps

- Customize email templates with your branding
- Add email verification on registration
- Implement email notifications for important events
- Add unsubscribe functionality for promotional emails
