using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Stratis.Bitcoin.Controllers;
using Stratis.Bitcoin.Controllers.Models;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.RPC.Exceptions;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Stratis.Bitcoin.Features.Wallet
{
    public class WalletRPCController : FeatureController
    {
        public WalletRPCController(IServiceProvider serviceProvider, IWalletManager walletManager)
        {
            this.WalletManager = walletManager;
            this.serviceProvider = serviceProvider;
        }

        internal IServiceProvider serviceProvider;

        public IWalletManager WalletManager { get; set; }

        [ActionName("decoderawtransaction")]
        [ActionDescription("Produces a human-readable JSON object for a raw transaction in hex format.")]
        public TransactionVerboseModel DecodeRawTransaction(string rawHex)
        {
            try
            {
                return this.WalletManager.DecodeRawTransaction(rawHex);
            }
            catch (Exception)
            {
                throw new RPCServerException(RPCErrorCode.RPC_INVALID_REQUEST, "Invalid raw transaction");
            }
        }

        [ActionName("getaccount")]
        [ActionDescription("Gets the account associated with the given address.")]
        public WalletAccountReference GetAccount(BitcoinAddress bitcoinAddress)
        {
            foreach (string walletName in this.WalletManager.GetWalletsNames())
            {
                foreach (HdAccount account in this.WalletManager.GetAccounts(walletName))
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        if (address.Address.Equals(bitcoinAddress.ToString()))
                            return new WalletAccountReference(walletName, account.Name);
                    }
                }
            }

            return null;
        }

        [ActionName("getnewaddress")]
        [ActionDescription("Gets an unused address from the wallet.")]
        public BitcoinAddress GetNewAddress()
        {
            // Use the first available wallet and account, in the absence of RPC credential mapping.
            string w = this.WalletManager.GetWalletsNames().FirstOrDefault();

            if (w == null)
                throw new RPCServerException(RPCErrorCode.RPC_INVALID_REQUEST, "No wallet found");

            HdAccount account = this.WalletManager.GetAccounts(w).FirstOrDefault();

            if (account == null)
                throw new RPCServerException(RPCErrorCode.RPC_INVALID_REQUEST, "No account found");

            return BitcoinAddress.Create(account.GetFirstUnusedReceivingAddress().Address, this.Network);
        }

        [ActionName("sendtoaddress")]
        [ActionDescription("Sends money to a bitcoin address.")]
        public uint256 SendToAddress(BitcoinAddress bitcoinAddress, Money amount)
        {
            throw new NotImplementedException("Need wallet credentials to be provided to the RPC controller for this to work.");
        }
    }
}
