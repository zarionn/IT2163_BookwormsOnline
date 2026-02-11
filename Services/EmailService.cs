using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

public class EmailService
{
    private readonly IConfiguration _configuration;
    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Pull variables from appsettings.json
        var fromEmail = _configuration["EmailSettings:FromEmail"];
        var appPassword = _configuration["EmailSettings:Password"];
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Bookworms Online", fromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

        using var client = new SmtpClient();
        try
        {
            // Use the variables for connection and authentication
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromEmail, appPassword);

            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EMAIL ERROR: {ex.Message}");
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}