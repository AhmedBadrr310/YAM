﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.AuthService.Core.Dtos
{
    public class VerifyDto
    {
        public string UserId { get; set; }

        public string Code { get; set; }
    }
}
