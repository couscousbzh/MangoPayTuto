﻿using MangoPay.SDK.Core.Enumerations;
using System;

namespace MangoPay.SDK.Entities.POST
{
    /// <summary>US bank account POST entity.</summary>
    public class BankAccountUsPostDTO : BankAccountPostDTO
    {
        public BankAccountUsPostDTO(String ownerName, String ownerAddress, String accountNumber, String aba)
        {
            Type = BankAccountType.US;
            OwnerName = ownerName;
            OwnerAddress = ownerAddress;
            AccountNumber = accountNumber;
            ABA = aba;
        }

        /// <summary>Account number.</summary>
        public String AccountNumber { get; set; }

        /// <summary>ABA.</summary>
        public String ABA { get; set; }
    }
}
