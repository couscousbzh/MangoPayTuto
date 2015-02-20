﻿using Common.Logging.Simple;
using MangoPay.SDK.Core.Enumerations;
using MangoPay.SDK.Entities;
using MangoPay.SDK.Entities.GET;
using MangoPay.SDK.Entities.POST;
using MangoPay.SDK.Entities.PUT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

namespace MangoPay.SDK.Tests
{
    /// <summary>Base abstract class for tests.</summary>
    public abstract class BaseTest
    {
        /// <summary>The MangoPayApi instance.</summary>
        protected MangoPayApi Api;

        private static UserNaturalDTO _john;
        private static UserLegalDTO _matrix;
        private static BankAccountIbanDTO _johnsAccount;
        private static WalletDTO _johnsWallet;
        private static WalletDTO _johnsWalletWithMoney;
        private static PayInCardWebDTO _johnsPayInCardWeb;
        private static PayOutBankWireDTO _johnsPayOutBankWire;
        private static CardRegistrationDTO _johnsCardRegistration;
        private static KycDocumentDTO _johnsKycDocument;
        private static PayOutBankWireDTO _johnsPayOutForCardDirect;
        private static HookDTO _johnsHook;

        public BaseTest()
        {
            this.Api = BuildNewMangoPayApi();
        }

        protected MangoPayApi BuildNewMangoPayApi()
        {
            MangoPayApi api = new MangoPayApi();

            // use test client credentails
            api.Config.ClientId = "sdk-unit-tests";
            api.Config.ClientPassword = "cqFfFrWfCcb7UadHNxx2C9Lo6Djw8ZduLi7J9USTmu8bhxxpju";
            api.Config.BaseUrl = "https://api.sandbox.mangopay.com";
            api.Config.LoggerFactoryAdapter = new ConsoleOutLoggerFactoryAdapter();

            // register storage strategy for tests
            api.OAuthTokenManager.RegisterCustomStorageStrategy(new DefaultStorageStrategyForTests());

            return api;
        }

        protected UserNaturalDTO GetJohn()
        {
            if (BaseTest._john == null)
            {
                UserNaturalPostDTO user = new UserNaturalPostDTO("john.doe@sample.org", "John", "Doe", new DateTime(1975, 12, 21, 0, 0, 0), CountryIso.FR, CountryIso.FR);
                user.Occupation = "programmer";
                user.IncomeRange = 3;
                user.Address = "Some Address";

                BaseTest._john = this.Api.Users.Create(user);
            }
            return BaseTest._john;
        }

        protected UserNaturalDTO GetNewJohn()
        {
            UserNaturalPostDTO user = new UserNaturalPostDTO("john.doe@sample.org", "John", "Doe", new DateTime(1975, 12, 21, 0, 0, 0), CountryIso.FR, CountryIso.FR);
            user.Occupation = "programmer";
            user.IncomeRange = 3;
            user.Address = "Some Address";

            return this.Api.Users.Create(user);
        }

        protected UserLegalDTO GetMatrix()
        {
            if (BaseTest._matrix == null)
            {
                UserNaturalDTO john = this.GetJohn();
                UserLegalPostDTO user = new UserLegalPostDTO(john.Email, "MartixSampleOrg", LegalPersonType.BUSINESS, john.FirstName, john.LastName, john.Birthday, john.Nationality, john.CountryOfResidence);
                user.HeadquartersAddress = "Some Address";
                user.LegalRepresentativeAddress = john.Address;
                user.LegalRepresentativeEmail = john.Email;
                user.LegalRepresentativeBirthday = new DateTime(1975, 12, 21, 0, 0, 0);
                user.Email = john.Email;

                BaseTest._matrix = this.Api.Users.Create(user);
            }
            return BaseTest._matrix;
        }

        protected BankAccountIbanDTO GetJohnsAccount()
        {
            if (BaseTest._johnsAccount == null)
            {
                UserNaturalDTO john = this.GetJohn();
                BankAccountIbanPostDTO account = new BankAccountIbanPostDTO(john.FirstName + " " + john.LastName, john.Address, "FR76 1790 6000 3200 0833 5232 973");
                account.UserId = john.Id;
                account.BIC = "BINAADADXXX";
                BaseTest._johnsAccount = this.Api.Users.CreateBankAccountIban(john.Id, account);
            }
            return BaseTest._johnsAccount;
        }

        protected WalletDTO GetJohnsWallet()
        {
            if (BaseTest._johnsWallet == null)
            {
                UserNaturalDTO john = this.GetJohn();

                WalletPostDTO wallet = new WalletPostDTO(new List<string> { john.Id }, "WALLET IN EUR", CurrencyIso.EUR);

                BaseTest._johnsWallet = this.Api.Wallets.Create(wallet);
            }

            return BaseTest._johnsWallet;
        }


        protected WalletDTO CreateJohnsWallet()
        {

            UserNaturalDTO john = this.GetJohn();

            WalletPostDTO wallet = new WalletPostDTO(new List<string> { john.Id }, "WALLET IN EUR", CurrencyIso.EUR);

            return Api.Wallets.Create(wallet);

        }

        /// <summary>Creates wallet for John, loaded with 10k EUR (John's got lucky) if not created yet, or returns an existing one.</summary>
        /// <returns>Wallet instance loaded with 10k EUR.</returns>
        protected WalletDTO GetJohnsWalletWithMoney()
        {
            return GetJohnsWalletWithMoney(10000);
        }

        /// <summary>Creates wallet for John, if not created yet, or returns an existing one.</summary>
        /// <param name="amount">Initial wallet's money amount.</param>
        /// <returns>Wallet entity instance returned from API.</returns>
        protected WalletDTO GetJohnsWalletWithMoney(int amount)
        {
            if (BaseTest._johnsWalletWithMoney == null)
            {
                UserNaturalDTO john = this.GetJohn();

                // create wallet with money
                WalletPostDTO wallet = new WalletPostDTO(new List<string> { john.Id }, "WALLET IN EUR WITH MONEY", CurrencyIso.EUR);

                BaseTest._johnsWalletWithMoney = this.Api.Wallets.Create(wallet);

                CardRegistrationPostDTO cardRegistrationPost = new CardRegistrationPostDTO(BaseTest._johnsWalletWithMoney.Owners[0], CurrencyIso.EUR);
                CardRegistrationDTO cardRegistration = this.Api.CardRegistrations.Create(cardRegistrationPost);

                CardRegistrationPutDTO cardRegistrationPut = new CardRegistrationPutDTO();
                cardRegistrationPut.RegistrationData = this.GetPaylineCorrectRegistartionData(cardRegistration);
                cardRegistration = this.Api.CardRegistrations.Update(cardRegistrationPut, cardRegistration.Id);

                CardDTO card = this.Api.Cards.Get(cardRegistration.CardId);

                // create pay-in CARD DIRECT
                PayInCardDirectPostDTO payIn = new PayInCardDirectPostDTO(cardRegistration.UserId, cardRegistration.UserId,
                    new Money { Amount = amount, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR },
                    BaseTest._johnsWalletWithMoney.Id, "http://test.com", card.Id);

                if (card.CardType == CardType.CB || card.CardType == CardType.VISA || card.CardType == CardType.MASTERCARD || card.CardType == CardType.CB_VISA_MASTERCARD)
                    payIn.CardType = CardType.CB_VISA_MASTERCARD;
                else if (card.CardType == CardType.AMEX)
                    payIn.CardType = CardType.AMEX;

                // create Pay-In
                this.Api.PayIns.CreateCardDirect(payIn);
            }

            return this.Api.Wallets.Get(BaseTest._johnsWalletWithMoney.Id);
        }

        protected PayInCardWebDTO GetJohnsPayInCardWeb()
        {
            if (BaseTest._johnsPayInCardWeb == null)
            {
                WalletDTO wallet = this.GetJohnsWallet();
                UserNaturalDTO user = this.GetJohn();

                PayInCardWebPostDTO payIn = new PayInCardWebPostDTO(user.Id, new Money { Amount = 1000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR }, wallet.Id, "https://test.com", CountryIso.FR, CardType.CB_VISA_MASTERCARD);

                BaseTest._johnsPayInCardWeb = this.Api.PayIns.CreateCardWeb(payIn);
            }

            return BaseTest._johnsPayInCardWeb;
        }

        protected PayInCardWebDTO GetJohnsPayInCardWeb(string walletId)
        {
            if (BaseTest._johnsPayInCardWeb == null)
            {
                UserNaturalDTO user = this.GetJohn();

                PayInCardWebPostDTO payIn = new PayInCardWebPostDTO(user.Id, new Money { Amount = 1000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR }, walletId, "https://test.com", CountryIso.FR, CardType.CB_VISA_MASTERCARD);

                BaseTest._johnsPayInCardWeb = this.Api.PayIns.CreateCardWeb(payIn);
            }

            return BaseTest._johnsPayInCardWeb;
        }

        protected PayInCardWebDTO CreateJohnsPayInCardWeb(string walletId)
        {

            UserNaturalDTO user = this.GetJohn();

            PayInCardWebPostDTO payIn = new PayInCardWebPostDTO(user.Id, new Money { Amount = 1000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR }, walletId, "https://test.com", CountryIso.FR, CardType.CB_VISA_MASTERCARD);

            return this.Api.PayIns.CreateCardWeb(payIn);
        }

        protected PayInCardWebDTO GetNewPayInCardWeb()
        {
            WalletDTO wallet = this.GetJohnsWallet();
            UserNaturalDTO user = this.GetJohn();

            PayInCardWebPostDTO payIn = new PayInCardWebPostDTO(user.Id, new Money { Amount = 1000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR }, wallet.Id, "https://test.com", CountryIso.FR, CardType.CB_VISA_MASTERCARD);

            BaseTest._johnsPayInCardWeb = this.Api.PayIns.CreateCardWeb(payIn);

            return BaseTest._johnsPayInCardWeb;
        }

        protected PayInCardDirectDTO GetNewPayInCardDirect()
        {
            return GetNewPayInCardDirect(null);
        }

        /// <summary>Creates PayIn Card Direct object.</summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>PayIn Card Direct instance returned from API.</returns>
        protected PayInCardDirectDTO GetNewPayInCardDirect(String userId)
        {
            WalletDTO wallet = this.GetJohnsWalletWithMoney();

            if (userId == null)
            {
                UserNaturalDTO user = this.GetJohn();
                userId = user.Id;
            }

            CardRegistrationPostDTO cardRegistrationPost = new CardRegistrationPostDTO(userId, CurrencyIso.EUR);

            CardRegistrationDTO cardRegistration = this.Api.CardRegistrations.Create(cardRegistrationPost);

            CardRegistrationPutDTO cardRegistrationPut = new CardRegistrationPutDTO();
            cardRegistrationPut.RegistrationData = this.GetPaylineCorrectRegistartionData(cardRegistration);
            cardRegistration = this.Api.CardRegistrations.Update(cardRegistrationPut, cardRegistration.Id);

            CardDTO card = this.Api.Cards.Get(cardRegistration.CardId);

            // create pay-in CARD DIRECT
            PayInCardDirectPostDTO payIn = new PayInCardDirectPostDTO(cardRegistration.UserId, cardRegistration.UserId,
                    new Money { Amount = 10000, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR },
                    wallet.Id, "http://test.com", card.Id);

            // payment type as CARD
            if (card.CardType == CardType.CB || card.CardType == CardType.VISA || card.CardType == CardType.MASTERCARD || card.CardType == CardType.CB_VISA_MASTERCARD)
                payIn.CardType = CardType.CB_VISA_MASTERCARD;
            else if (card.CardType == CardType.AMEX)
                payIn.CardType = CardType.AMEX;

            return this.Api.PayIns.CreateCardDirect(payIn);
        }

        protected PayOutBankWireDTO GetJohnsPayOutBankWire()
        {
            if (BaseTest._johnsPayOutBankWire == null)
            {
                WalletDTO wallet = this.GetJohnsWallet();
                UserNaturalDTO user = this.GetJohn();
                BankAccountDTO account = this.GetJohnsAccount();

                PayOutBankWirePostDTO payOut = new PayOutBankWirePostDTO(user.Id, wallet.Id, new Money { Amount = 10, Currency = CurrencyIso.EUR }, new Money { Amount = 5, Currency = CurrencyIso.EUR }, account.Id);
                payOut.Tag = "DefaultTag";
                payOut.CreditedUserId = user.Id;
                payOut.Communication = "Communication text";

                BaseTest._johnsPayOutBankWire = this.Api.PayOuts.CreateBankWire(payOut);
            }

            return BaseTest._johnsPayOutBankWire;
        }

        /// <summary>Creates PayOut Bank Wire object.</summary>
        /// <returns>PayOut Bank Wire instance returned from API.</returns>
        protected PayOutBankWireDTO GetJohnsPayOutForCardDirect()
        {
            if (BaseTest._johnsPayOutForCardDirect == null)
            {
                PayInCardDirectDTO payIn = this.GetNewPayInCardDirect();
                BankAccountDTO account = this.GetJohnsAccount();

                PayOutBankWirePostDTO payOut = new PayOutBankWirePostDTO(payIn.AuthorId, payIn.CreditedWalletId, new Money { Amount = 10, Currency = CurrencyIso.EUR },
                    new Money { Amount = 5, Currency = CurrencyIso.EUR }, account.Id);
                payOut.Tag = "DefaultTag";
                payOut.CreditedUserId = payIn.AuthorId;
                payOut.Communication = "Communication text";

                BaseTest._johnsPayOutForCardDirect = this.Api.PayOuts.CreateBankWire(payOut);
            }

            return BaseTest._johnsPayOutForCardDirect;
        }

        protected TransferDTO GetNewTransfer()
        {
            WalletDTO walletWithMoney = this.GetJohnsWalletWithMoney();
            UserNaturalDTO user = this.GetJohn();

            WalletPostDTO walletPost = new WalletPostDTO(new List<string> { user.Id }, "WALLET IN EUR FOR TRANSFER", CurrencyIso.EUR);
            WalletDTO wallet = this.Api.Wallets.Create(walletPost);

            TransferPostDTO transfer = new TransferPostDTO(user.Id, user.Id, new Money { Amount = 100, Currency = CurrencyIso.EUR }, new Money { Amount = 0, Currency = CurrencyIso.EUR }, walletWithMoney.Id, wallet.Id);
            transfer.Tag = "DefaultTag";

            return this.Api.Transfers.Create(transfer);
        }

        /// <summary>Creates refund object for transfer.</summary>
        /// <param name="transfer">Transfer.</param>
        /// <returns>Refund instance returned from API.</returns>
        protected RefundDTO GetNewRefundForTransfer(TransferDTO transfer)
        {
            UserNaturalDTO user = this.GetJohn();

            RefundTransferPostDTO refund = new RefundTransferPostDTO(user.Id);

            return this.Api.Transfers.CreateRefund(transfer.Id, refund);
        }

        /// <summary>Creates refund object for PayIn.</summary>
        /// <param name="payIn">PayIn entity.</param>
        /// <returns>Refund instance returned from API.</returns>
        protected RefundDTO GetNewRefundForPayIn(PayInDTO payIn)
        {
            UserNaturalDTO user = this.GetJohn();

            Money debitedFunds = new Money();
            debitedFunds.Amount = payIn.DebitedFunds.Amount;
            debitedFunds.Currency = payIn.DebitedFunds.Currency;
            Money fees = new Money();
            fees.Amount = payIn.Fees.Amount;
            fees.Currency = payIn.Fees.Currency;

            RefundPayInPostDTO refund = new RefundPayInPostDTO(user.Id, fees, debitedFunds);

            return this.Api.PayIns.CreateRefund(payIn.Id, refund);
        }

        /// <summary>Creates card registration object.</summary>
        /// <returns>CardRegistration instance returned from API.</returns>
        protected CardRegistrationDTO GetJohnsCardRegistration()
        {
            if (BaseTest._johnsCardRegistration == null)
            {
                UserNaturalDTO user = this.GetJohn();

                CardRegistrationPostDTO cardRegistration = new CardRegistrationPostDTO(user.Id, CurrencyIso.EUR);

                BaseTest._johnsCardRegistration = this.Api.CardRegistrations.Create(cardRegistration);
            }

            return BaseTest._johnsCardRegistration;
        }

        /// <summary>Creates card registration object.</summary>
        /// <returns>CardPreAuthorization instance returned from API.</returns>
        protected CardPreAuthorizationDTO GetJohnsCardPreAuthorization()
        {
            UserNaturalDTO user = this.GetJohn();
            CardRegistrationPostDTO cardRegistrationPost = new CardRegistrationPostDTO(user.Id, CurrencyIso.EUR);
            CardRegistrationDTO newCardRegistration = this.Api.CardRegistrations.Create(cardRegistrationPost);

            CardRegistrationPutDTO cardRegistrationPut = new CardRegistrationPutDTO();
            String registrationData = this.GetPaylineCorrectRegistartionData(newCardRegistration);
            cardRegistrationPut.RegistrationData = registrationData;
            CardRegistrationDTO getCardRegistration = this.Api.CardRegistrations.Update(cardRegistrationPut, newCardRegistration.Id);

            CardPreAuthorizationPostDTO cardPreAuthorization = new CardPreAuthorizationPostDTO(user.Id, new Money { Amount = 10000, Currency = CurrencyIso.EUR }, SecureMode.DEFAULT, getCardRegistration.CardId, "http://test.com");

            return this.Api.CardPreAuthorizations.Create(cardPreAuthorization);
        }

        protected KycDocumentDTO GetJohnsKycDocument()
        {
            if (BaseTest._johnsKycDocument == null)
            {
                String johnsId = this.GetJohn().Id;

                BaseTest._johnsKycDocument = this.Api.Users.CreateKycDocument(johnsId, KycDocumentType.IDENTITY_PROOF);
            }

            return BaseTest._johnsKycDocument;
        }

        protected KycDocumentDTO GetNewKycDocument()
        {
            BaseTest._johnsKycDocument = null;
            return GetJohnsKycDocument();
        }

        /// <summary>Gets registration data from Payline service.</summary>
        /// <param name="cardRegistration">CardRegistration instance.</param>
        /// <returns>Registration data.</returns>
        protected String GetPaylineCorrectRegistartionData(CardRegistrationDTO cardRegistration)
        {
            RestClient client = new RestClient(cardRegistration.CardRegistrationURL);

            RestRequest request = new RestRequest(Method.POST);
            request.AddParameter("data", cardRegistration.PreregistrationData);
            request.AddParameter("accessKeyRef", cardRegistration.AccessKey);
            request.AddParameter("cardNumber", "4970100000000154");
            request.AddParameter("cardExpirationDate", "1218");
            request.AddParameter("cardCvx", "123");

            IRestResponse response = client.Execute(request);

            String responseString = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
                return responseString;
            else
                throw new Exception(responseString);
        }

        protected HookDTO GetJohnsHook()
        {
            if (BaseTest._johnsHook == null)
            {

                Pagination pagination = new Pagination(1, 1);
                ListPaginated<HookDTO> list = this.Api.Hooks.GetAll(pagination);

                if (list != null && list.Count > 0 && list[0] != null)
                {
                    BaseTest._johnsHook = list[0];
                }
                else
                {
                    HookPostDTO hook = new HookPostDTO("http://test.com", EventType.PAYIN_NORMAL_CREATED);
                    BaseTest._johnsHook = this.Api.Hooks.Create(hook);
                }
            }

            return BaseTest._johnsHook;
        }

        protected void AssertEqualInputProps<T>(T entity1, T entity2)
        {
            Assert.IsNotNull(entity1);
            Assert.IsNotNull(entity2);

            if (entity1 is UserNaturalDTO && entity2 is UserNaturalDTO)
            {
                Assert.AreEqual((entity1 as UserNaturalDTO).Tag, (entity2 as UserNaturalDTO).Tag);
                Assert.AreEqual((entity1 as UserNaturalDTO).PersonType, (entity2 as UserNaturalDTO).PersonType);
                Assert.AreEqual((entity1 as UserNaturalDTO).FirstName, (entity2 as UserNaturalDTO).FirstName);
                Assert.AreEqual((entity1 as UserNaturalDTO).LastName, (entity2 as UserNaturalDTO).LastName);
                Assert.AreEqual((entity1 as UserNaturalDTO).Email, (entity2 as UserNaturalDTO).Email);
                Assert.AreEqual((entity1 as UserNaturalDTO).Address, (entity2 as UserNaturalDTO).Address);
                Assert.AreEqual((entity1 as UserNaturalDTO).Birthday, (entity2 as UserNaturalDTO).Birthday);
                Assert.AreEqual((entity1 as UserNaturalDTO).Nationality, (entity2 as UserNaturalDTO).Nationality);
                Assert.AreEqual((entity1 as UserNaturalDTO).CountryOfResidence, (entity2 as UserNaturalDTO).CountryOfResidence);
                Assert.AreEqual((entity1 as UserNaturalDTO).Occupation, (entity2 as UserNaturalDTO).Occupation);
                Assert.AreEqual((entity1 as UserNaturalDTO).IncomeRange, (entity2 as UserNaturalDTO).IncomeRange);
            }
            else if (entity1 is UserLegalDTO && entity2 is UserLegalDTO)
            {
                Assert.AreEqual((entity1 as UserLegalDTO).Tag, (entity2 as UserLegalDTO).Tag);
                Assert.AreEqual((entity1 as UserLegalDTO).PersonType, (entity2 as UserLegalDTO).PersonType);
                Assert.AreEqual((entity1 as UserLegalDTO).Name, (entity2 as UserLegalDTO).Name);
                Assert.AreEqual((entity1 as UserLegalDTO).HeadquartersAddress, (entity2 as UserLegalDTO).HeadquartersAddress);
                Assert.AreEqual((entity1 as UserLegalDTO).LegalRepresentativeFirstName, (entity2 as UserLegalDTO).LegalRepresentativeFirstName);
                Assert.AreEqual((entity1 as UserLegalDTO).LegalRepresentativeLastName, (entity2 as UserLegalDTO).LegalRepresentativeLastName);
                //Assert.AreEqual("***** TEMPORARY API ISSUE: RETURNED OBJECT MISSES THIS PROP AFTER CREATION *****", (entity1 as UserLegal).LegalRepresentativeAddress, (entity2 as UserLegal).LegalRepresentativeAddress);
                Assert.AreEqual((entity1 as UserLegalDTO).LegalRepresentativeEmail, (entity2 as UserLegalDTO).LegalRepresentativeEmail);
                //Assert.AreEqual("***** TEMPORARY API ISSUE: RETURNED OBJECT HAS THIS PROP CHANGED FROM TIMESTAMP INTO ISO STRING AFTER CREATION *****", (entity1 as UserLegal).LegalRepresentativeBirthday, (entity2 as UserLegal).LegalRepresentativeBirthday);
                Assert.AreEqual((entity1 as UserLegalDTO).LegalRepresentativeBirthday, (entity2 as UserLegalDTO).LegalRepresentativeBirthday);
                Assert.AreEqual((entity1 as UserLegalDTO).LegalRepresentativeNationality, (entity2 as UserLegalDTO).LegalRepresentativeNationality);
                Assert.AreEqual((entity1 as UserLegalDTO).LegalRepresentativeCountryOfResidence, (entity2 as UserLegalDTO).LegalRepresentativeCountryOfResidence);
            }
            else if (entity1 is BankAccountDTO && entity2 is BankAccountDTO)
            {
                Assert.AreEqual((entity1 as BankAccountDTO).Tag, (entity2 as BankAccountDTO).Tag);
                Assert.AreEqual((entity1 as BankAccountDTO).UserId, (entity2 as BankAccountDTO).UserId);
                Assert.AreEqual((entity1 as BankAccountDTO).Type, (entity2 as BankAccountDTO).Type);
                Assert.AreEqual((entity1 as BankAccountDTO).OwnerName, (entity2 as BankAccountDTO).OwnerName);
                Assert.AreEqual((entity1 as BankAccountDTO).OwnerAddress, (entity2 as BankAccountDTO).OwnerAddress);
                if ((entity1 as BankAccountDTO).Type == BankAccountType.IBAN)
                {
                    Assert.AreEqual((entity1 as BankAccountIbanDTO).IBAN, (entity2 as BankAccountIbanDTO).IBAN);
                    Assert.AreEqual((entity1 as BankAccountIbanDTO).BIC, (entity2 as BankAccountIbanDTO).BIC);
                }
                else if ((entity1 as BankAccountDTO).Type == BankAccountType.GB)
                {
                    Assert.AreEqual((entity1 as BankAccountGbDTO).AccountNumber, (entity2 as BankAccountGbDTO).AccountNumber);
                    Assert.AreEqual((entity1 as BankAccountGbDTO).SortCode, (entity2 as BankAccountGbDTO).SortCode);
                }
                else if ((entity1 as BankAccountDTO).Type == BankAccountType.US)
                {
                    Assert.AreEqual((entity1 as BankAccountUsDTO).AccountNumber, (entity2 as BankAccountUsDTO).AccountNumber);
                    Assert.AreEqual((entity1 as BankAccountUsDTO).ABA, (entity2 as BankAccountUsDTO).ABA);
                }
                else if ((entity1 as BankAccountDTO).Type == BankAccountType.CA)
                {
                    Assert.AreEqual((entity1 as BankAccountCaDTO).AccountNumber, (entity2 as BankAccountCaDTO).AccountNumber);
                    Assert.AreEqual((entity1 as BankAccountCaDTO).BankName, (entity2 as BankAccountCaDTO).BankName);
                    Assert.AreEqual((entity1 as BankAccountCaDTO).InstitutionNumber, (entity2 as BankAccountCaDTO).InstitutionNumber);
                    Assert.AreEqual((entity1 as BankAccountCaDTO).BranchCode, (entity2 as BankAccountCaDTO).BranchCode);
                }
                else if ((entity1 as BankAccountDTO).Type == BankAccountType.OTHER)
                {
                    Assert.AreEqual((entity1 as BankAccountOtherDTO).AccountNumber, (entity2 as BankAccountOtherDTO).AccountNumber);
                    Assert.AreEqual((entity1 as BankAccountOtherDTO).Type, (entity2 as BankAccountOtherDTO).Type);
                    Assert.AreEqual((entity1 as BankAccountOtherDTO).Country, (entity2 as BankAccountOtherDTO).Country);
                    Assert.AreEqual((entity1 as BankAccountOtherDTO).BIC, (entity2 as BankAccountOtherDTO).BIC);
                }
            }
            else if (entity1 is PayInDTO && entity2 is PayInDTO)
            {
                Assert.AreEqual((entity1 as PayInDTO).Tag, (entity2 as PayInDTO).Tag);
                Assert.AreEqual((entity1 as PayInDTO).AuthorId, (entity2 as PayInDTO).AuthorId);
                Assert.AreEqual((entity1 as PayInDTO).CreditedUserId, (entity2 as PayInDTO).CreditedUserId);

                AssertEqualInputProps((entity1 as PayInDTO).DebitedFunds, (entity2 as PayInDTO).DebitedFunds);
                AssertEqualInputProps((entity1 as PayInDTO).CreditedFunds, (entity2 as PayInDTO).CreditedFunds);
                AssertEqualInputProps((entity1 as PayInDTO).Fees, (entity2 as PayInDTO).Fees);
            }
            else if (typeof(T) == typeof(CardDTO))
            {
                Assert.AreEqual((entity1 as CardDTO).ExpirationDate, (entity2 as CardDTO).ExpirationDate);
                Assert.AreEqual((entity1 as CardDTO).Alias, (entity2 as CardDTO).Alias);
                Assert.AreEqual((entity1 as CardDTO).CardType, (entity2 as CardDTO).CardType);
                Assert.AreEqual((entity1 as CardDTO).Currency, (entity2 as CardDTO).Currency);
            }
            else if (typeof(T) == typeof(PayOutDTO))
            {
                Assert.AreEqual((entity1 as PayOutDTO).Tag, (entity2 as PayOutDTO).Tag);
                Assert.AreEqual((entity1 as PayOutDTO).AuthorId, (entity2 as PayOutDTO).AuthorId);
                Assert.AreEqual((entity1 as PayOutDTO).CreditedUserId, (entity2 as PayOutDTO).CreditedUserId);

                AssertEqualInputProps((entity1 as PayOutDTO).DebitedFunds, (entity2 as PayOutDTO).DebitedFunds);
                AssertEqualInputProps((entity1 as PayOutDTO).CreditedFunds, (entity2 as PayOutDTO).CreditedFunds);
                AssertEqualInputProps((entity1 as PayOutDTO).Fees, (entity2 as PayOutDTO).Fees);
            }
            else if (typeof(T) == typeof(TransferDTO))
            {
                Assert.AreEqual((entity1 as TransferDTO).Tag, (entity2 as TransferDTO).Tag);
                Assert.AreEqual((entity1 as TransferDTO).AuthorId, (entity2 as TransferDTO).AuthorId);
                Assert.AreEqual((entity1 as TransferDTO).CreditedUserId, (entity2 as TransferDTO).CreditedUserId);

                AssertEqualInputProps((entity1 as TransferDTO).DebitedFunds, (entity2 as TransferDTO).DebitedFunds);
                AssertEqualInputProps((entity1 as TransferDTO).CreditedFunds, (entity2 as TransferDTO).CreditedFunds);
                AssertEqualInputProps((entity1 as TransferDTO).Fees, (entity2 as TransferDTO).Fees);
            }
            else if (typeof(T) == typeof(TransactionDTO))
            {
                Assert.AreEqual((entity1 as TransactionDTO).Tag, (entity2 as TransactionDTO).Tag);

                AssertEqualInputProps((entity1 as TransactionDTO).DebitedFunds, (entity2 as TransactionDTO).DebitedFunds);
                AssertEqualInputProps((entity1 as TransactionDTO).Fees, (entity2 as TransactionDTO).Fees);
                AssertEqualInputProps((entity1 as TransactionDTO).CreditedFunds, (entity2 as TransactionDTO).CreditedFunds);

                Assert.AreEqual((entity1 as TransactionDTO).Status, (entity2 as TransactionDTO).Status);
            }
            else if (typeof(T) == typeof(Money))
            {
                Assert.AreEqual((entity1 as Money).Currency, (entity2 as Money).Currency);
                Assert.AreEqual((entity1 as Money).Amount, (entity2 as Money).Amount);
            }
            else
            {
                throw new ArgumentException("Unsupported type.");
            }
        }
    }
}
