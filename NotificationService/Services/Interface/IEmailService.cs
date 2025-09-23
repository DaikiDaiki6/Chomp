using System;
using NotificationService.Models;

namespace NotificationService.Services.Interface;

public interface IEmailService
{
    Task TestConsoleWriteLineForNow(Notification notification, string email);
}
