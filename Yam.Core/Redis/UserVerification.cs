using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.Redis
{
    public class UserVerification
    {
        public string UserId { get; set; }

        public string VerificationCode { get; set; }
    }
}
