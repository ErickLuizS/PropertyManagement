// Path: Services/AmazonSesEmailService.cs
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace PropertyManagement.Services
{
    public class AmazonSesEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AmazonSesEmailService> _logger;
        private readonly IAmazonSimpleEmailService _sesClient;

        public AmazonSesEmailService(IConfiguration configuration, ILogger<AmazonSesEmailService> logger, IAmazonSimpleEmailService sesClient)
        {
            _configuration = configuration;
            _logger = logger;
            _sesClient = sesClient;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var sendRequest = new SendEmailRequest
            {
                Source = _configuration["EmailSettings:FromEmail"],
                Destination = new Destination { ToAddresses = new List<string> { toEmail } },
                Message = new Message
                {
                    Subject = new Amazon.SimpleEmail.Model.Content(subject),
                    Body = new Body(new Amazon.SimpleEmail.Model.Content(message))
                }
            };

            var response = await _sesClient.SendEmailAsync(sendRequest);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                _logger.LogInformation($"Amazon SES email sent to {toEmail}");
            else
                _logger.LogError("Failed to send email via Amazon SES.");
        }
    }
}
