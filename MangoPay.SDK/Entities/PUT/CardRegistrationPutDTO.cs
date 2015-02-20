﻿using MangoPay.SDK.Core;
using System;

namespace MangoPay.SDK.Entities.PUT
{
    /// <summary>Card registration PUT entity.</summary>
    public class CardRegistrationPutDTO : EntityPutBase
    {
        /// <summary>Registration data.</summary>
        public String RegistrationData { get; set; }
    }
}
