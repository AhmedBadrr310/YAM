using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.NotificationService.Core.Interfaces
{
    public interface IMailService
    {
        public Task SendVerificationMail(string email, string verificationCode);
    }
}
