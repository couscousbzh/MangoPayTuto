﻿using MangoPay.SDK.Core;
using MangoPay.SDK.Core.Enumerations;
using MangoPay.SDK.Entities;
using MangoPay.SDK.Entities.GET;
using MangoPay.SDK.Entities.POST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MangoPay.SDK.Tests
{
    [TestClass]
    public class ApiPayInsTest : BaseTest
    {
        [TestMethod]
        public void Test_PayIns_Create_CardWeb()
        {
            try
            {
                PayInDTO payIn = null;
                payIn = this.GetJohnsPayInCardWeb();

                Assert.IsTrue(payIn.Id.Length > 0);
                Assert.IsTrue(payIn.PaymentType == PayInPaymentType.CARD);
                Assert.IsTrue(payIn.ExecutionType == PayInExecutionType.WEB);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_Get_CardWeb()
        {
            try
            {
                PayInDTO payIn = null;
                payIn = this.GetJohnsPayInCardWeb();

                PayInDTO getPayIn = this.Api.PayIns.Get(payIn.Id);

                Assert.IsTrue(payIn.Id == getPayIn.Id);
                Assert.IsTrue(payIn.PaymentType == PayInPaymentType.CARD);
                Assert.IsTrue(payIn.ExecutionType == PayInExecutionType.WEB);

                AssertEqualInputProps(payIn, getPayIn);

                Assert.IsTrue(getPayIn.Status == TransactionStatus.CREATED);
                Assert.IsTrue(getPayIn.ExecutionDate == null);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_Create_CardDirect()
        {
            try
            {
                WalletDTO johnWallet = this.GetJohnsWalletWithMoney();
                WalletDTO beforeWallet = this.Api.Wallets.Get(johnWallet.Id);

                PayInDTO payIn = this.GetNewPayInCardDirect();
                WalletDTO wallet = this.Api.Wallets.Get(johnWallet.Id);
                UserNaturalDTO user = this.GetJohn();

                Assert.IsTrue(payIn.Id.Length > 0);
                Assert.AreEqual(wallet.Id, payIn.CreditedWalletId);
                Assert.AreEqual(PayInPaymentType.CARD, payIn.PaymentType);
                Assert.AreEqual(PayInExecutionType.DIRECT, payIn.ExecutionType);
                Assert.IsTrue(payIn.DebitedFunds is Money);
                Assert.IsTrue(payIn.CreditedFunds is Money);
                Assert.IsTrue(payIn.Fees is Money);
                Assert.AreEqual(user.Id, payIn.AuthorId);
                Assert.IsTrue(wallet.Balance.Amount == beforeWallet.Balance.Amount + payIn.CreditedFunds.Amount);
                Assert.AreEqual(TransactionStatus.SUCCEEDED, payIn.Status);
                Assert.AreEqual(TransactionType.PAYIN, payIn.Type);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_Get_CardDirect()
        {
            try
            {
                PayInCardDirectDTO payIn = this.GetNewPayInCardDirect();

                PayInCardDirectDTO getPayIn = this.Api.PayIns.GetCardDirect(payIn.Id);

                Assert.IsTrue(payIn.Id == getPayIn.Id);
                Assert.IsTrue(payIn.PaymentType == PayInPaymentType.CARD);
                Assert.IsTrue(payIn.ExecutionType == PayInExecutionType.DIRECT);
                AssertEqualInputProps(payIn, getPayIn);
                Assert.IsNotNull(getPayIn.CardId);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_CreateRefund_CardDirect()
        {
            try
            {
                PayInDTO payIn = this.GetNewPayInCardDirect();
                WalletDTO wallet = this.GetJohnsWalletWithMoney();
                WalletDTO walletBefore = this.Api.Wallets.Get(wallet.Id);

                RefundDTO refund = this.GetNewRefundForPayIn(payIn);
                WalletDTO walletAfter = this.Api.Wallets.Get(wallet.Id);

                Assert.IsTrue(refund.Id.Length > 0);
                Assert.IsTrue(refund.DebitedFunds.Amount == payIn.DebitedFunds.Amount);
                Assert.IsTrue(walletBefore.Balance.Amount == (walletAfter.Balance.Amount + payIn.DebitedFunds.Amount));
                Assert.AreEqual(TransactionType.PAYOUT, refund.Type);
                Assert.AreEqual(TransactionNature.REFUND, refund.Nature);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_PreAuthorizedDirect()
        {
            try
            {
                CardPreAuthorizationDTO cardPreAuthorization = this.GetJohnsCardPreAuthorization();
                WalletDTO wallet = this.GetJohnsWalletWithMoney();
                UserNaturalDTO user = this.GetJohn();

                // create pay-in PRE-AUTHORIZED DIRECT
                PayInPreauthorizedDirectPostDTO payIn = new PayInPreauthorizedDirectPostDTO(user.Id, new Money { Amount = 10000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR }, wallet.Id, cardPreAuthorization.Id);
               
                payIn.SecureModeReturnURL = "http://test.com";

                PayInPreauthorizedDirectDTO createPayIn = this.Api.PayIns.CreatePreauthorizedDirect(payIn);

                Assert.IsTrue("" != createPayIn.Id);
                Assert.AreEqual(wallet.Id, createPayIn.CreditedWalletId);
                Assert.AreEqual(PayInPaymentType.PREAUTHORIZED, createPayIn.PaymentType);
                Assert.AreEqual(PayInExecutionType.DIRECT, createPayIn.ExecutionType);
                Assert.IsTrue(createPayIn.DebitedFunds is Money);
                Assert.IsTrue(createPayIn.CreditedFunds is Money);
                Assert.IsTrue(createPayIn.Fees is Money);
                Assert.AreEqual(user.Id, createPayIn.AuthorId);
                Assert.AreEqual(TransactionStatus.SUCCEEDED, createPayIn.Status);
                Assert.AreEqual(TransactionType.PAYIN, createPayIn.Type);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_BankWireDirect_Create()
        {
            try
            {
                WalletDTO wallet = this.GetJohnsWallet();
                UserNaturalDTO user = this.GetJohn();

                // create pay-in BANKWIRE DIRECT
                PayInBankWireDirectPostDTO payIn = new PayInBankWireDirectPostDTO(user.Id, wallet.Id, new Money { Amount = 10000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR });
                payIn.CreditedWalletId = wallet.Id;
                payIn.AuthorId = user.Id;

                PayInDTO createPayIn = this.Api.PayIns.CreateBankWireDirect(payIn);

                Assert.IsTrue(createPayIn.Id.Length > 0);
                Assert.AreEqual(wallet.Id, createPayIn.CreditedWalletId);
                Assert.AreEqual(PayInPaymentType.BANK_WIRE, createPayIn.PaymentType);
                Assert.AreEqual(PayInExecutionType.DIRECT, createPayIn.ExecutionType);
                Assert.AreEqual(user.Id, createPayIn.AuthorId);
                Assert.AreEqual(TransactionStatus.CREATED, createPayIn.Status);
                Assert.AreEqual(TransactionType.PAYIN, createPayIn.Type);
                Assert.IsNotNull(((PayInBankWireDirectDTO)createPayIn).WireReference);
                Assert.AreEqual(((PayInBankWireDirectDTO)createPayIn).BankAccount.Type, BankAccountType.IBAN);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_BankWireDirect_Get()
        {
            try
            {
                WalletDTO wallet = this.GetJohnsWallet();
                UserNaturalDTO user = this.GetJohn();

                // create pay-in BANKWIRE DIRECT
                PayInBankWireDirectPostDTO payIn = new PayInBankWireDirectPostDTO(user.Id, wallet.Id, new Money { Amount = 10000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR });
                payIn.CreditedWalletId = wallet.Id;
                payIn.AuthorId = user.Id;

                PayInBankWireDirectDTO createdPayIn = this.Api.PayIns.CreateBankWireDirect(payIn);

                PayInBankWireDirectDTO getPayIn = this.Api.PayIns.GetBankWireDirect(createdPayIn.Id);

                Assert.AreEqual(getPayIn.Id, createdPayIn.Id);
                Assert.AreEqual(PayInPaymentType.BANK_WIRE, getPayIn.PaymentType);
                Assert.AreEqual(PayInExecutionType.DIRECT, getPayIn.ExecutionType);
                Assert.AreEqual(user.Id, getPayIn.AuthorId);
                Assert.AreEqual(TransactionType.PAYIN, getPayIn.Type);
                Assert.IsNotNull(getPayIn.WireReference);
                Assert.AreEqual(getPayIn.BankAccount.Type, BankAccountType.IBAN);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_PayIns_DirectDebit_Create_Get()
        {
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();
            // create pay-in DIRECT DEBIT
            PayInDirectDebitPostDTO payIn = new PayInDirectDebitPostDTO(user.Id, new Money { Amount = 10000, Currency = CurrencyIso.EUR }, new Money { Amount = 100, Currency = CurrencyIso.EUR }, wallet.Id, "http://www.mysite.com/returnURL/", CountryIso.FR, DirectDebitType.GIROPAY);

            payIn.TemplateURLOptions = new TemplateURLOptions { PAYLINE = "https://www.maysite.com/payline_template/" };
            payIn.Tag = "DirectDebit test tag";

            PayInDirectDebitDTO createPayIn = this.Api.PayIns.CreateDirectDebit(payIn);

            Assert.IsNotNull(createPayIn);
            Assert.IsTrue(createPayIn.Id.Length > 0);
            Assert.AreEqual(wallet.Id, createPayIn.CreditedWalletId);
            Assert.IsTrue(createPayIn.PaymentType == PayInPaymentType.DIRECT_DEBIT);
            Assert.IsTrue(createPayIn.DirectDebitType == DirectDebitType.GIROPAY);
            Assert.IsTrue(createPayIn.Culture == CountryIso.FR);
            Assert.AreEqual(user.Id, createPayIn.AuthorId);
            Assert.IsTrue(createPayIn.Status == TransactionStatus.CREATED);
            Assert.IsTrue(createPayIn.Type == TransactionType.PAYIN);
            Assert.IsNotNull(createPayIn.DebitedFunds);
            Assert.IsTrue(createPayIn.DebitedFunds is Money);
            Assert.AreEqual(10000, createPayIn.DebitedFunds.Amount);
            Assert.IsTrue(createPayIn.DebitedFunds.Currency == CurrencyIso.EUR);

            Assert.IsNotNull(createPayIn.CreditedFunds);
            Assert.IsTrue(createPayIn.CreditedFunds is Money);
            Assert.AreEqual(9900, createPayIn.CreditedFunds.Amount);
            Assert.IsTrue(createPayIn.CreditedFunds.Currency == CurrencyIso.EUR);

            Assert.IsNotNull(createPayIn.Fees);
            Assert.IsTrue(createPayIn.Fees is Money);
            Assert.AreEqual(100, createPayIn.Fees.Amount);
            Assert.IsTrue(createPayIn.Fees.Currency == CurrencyIso.EUR);

            Assert.IsNotNull(createPayIn.ReturnURL);
            Assert.IsNotNull(createPayIn.RedirectURL);
            Assert.IsNotNull(createPayIn.TemplateURL);


            PayInDirectDebitDTO getPayIn = this.Api.PayIns.GetDirectDebit(createPayIn.Id);

            Assert.IsNotNull(getPayIn);
            Assert.IsTrue(getPayIn.Id == createPayIn.Id);
            Assert.IsTrue(getPayIn.Tag == createPayIn.Tag);
        }
    }
}
