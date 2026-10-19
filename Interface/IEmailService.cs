namespace Nirmaan.Api.Interface;

public interface IEmailService
{
    Task SendResetCodeAsync(string toEmail, string uniqueCode);
}