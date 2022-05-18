// Copyright (C) 2016-2021 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.CLI
{
    partial class MainService
    {
        
        // SocialLedger deployed address
        private readonly UInt160 slInvokeTokenHash = UInt160.Parse("0x4fed42809f613c0176fec151e7766e0e6aa1c9a8");

        /// <summary>
        /// Process "transfer" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        /// <param name="to">To</param>
        /// <param name="amount">Ammount</param>
        /// <param name="from">From</param>
        /// <param name="data">Data</param>
        /// <param name="signersAccounts">Signer's accounts</param>
        [ConsoleCommand("transfer", Category = "NEP17 Commands")]
        private void OnTransferCommand(UInt160 tokenHash, UInt160 to, decimal amount, UInt160 from = null, string data = null, UInt160[] signersAccounts = null)
        {
            var snapshot = NeoSystem.StoreView;
            var asset = new AssetDescriptor(snapshot, NeoSystem.Settings, tokenHash);
            var value = new BigDecimal(amount, asset.Decimals);

            if (NoWallet()) {
                return;
            }

            Transaction tx;
            try
            {
                tx = CurrentWallet.MakeTransaction(snapshot, new[]
                {
                    new TransferOutput
                    {
                        AssetId = tokenHash,
                        Value = value,
                        ScriptHash = to,
                        Data = data
                    }
                }, from: from, cosigners: signersAccounts?.Select(p => new Signer
                {
                    // default access for transfers should be valid only for first invocation
                    Scopes = WitnessScope.CalledByEntry,
                    Account = p
                })
                .ToArray() ?? new Signer[0]);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error($"{GetExceptionMessage(e)}: {e.ToString()}");
                return;
            }
            if (!ReadUserInput("Relay tx(no|yes)").IsYes())
            {
                return;
            }
            SignAndSendTx(snapshot, tx);
        }

        /// <summary>
        /// Process "transfera2h" command
        /// </summary>
        [ConsoleCommand("transfera2h", Category = "NEP17 Commands")]
        private void OnTransferFromAddressToHandle(UInt160 tokenHash, byte[] handle, decimal amount, UInt160 from, UInt160 sender, UInt160[] signerAccounts = null) {
            const decimal maxGas = 20;
            var gas = new BigDecimal(maxGas, NativeContract.GAS.Decimals);
            Signer[] signers = System.Array.Empty<Signer>();

            if (NoWallet()) {
                return;
            }

            if (signerAccounts == null) {
                signerAccounts = new UInt160[1] { sender };
            } else if (signerAccounts.Contains(sender) && signerAccounts[0] != sender) {
                var signersList = signerAccounts.ToList();
                signersList.Remove(sender);
                signerAccounts = signersList.Prepend(sender).ToArray();
            } else if (!signerAccounts.Contains(sender)) {
                signerAccounts = signerAccounts.Prepend(sender).ToArray();
            }

            signers = signerAccounts
                .Select(p => 
                    new Signer() {
                        Account = p,
                        Scopes = WitnessScope.CalledByEntry | WitnessScope.CustomContracts,
                        AllowedContracts = new[] { NativeContract.DVITA.Hash, NativeContract.GAS.Hash }
                    }
                )
                .ToArray();
            
            var tx = new Transaction {
                Signers = signers,
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Witnesses = System.Array.Empty<Witness>(),
            };

            // Get Decimals for scriptHash
            var asset = new AssetDescriptor(NeoSystem.StoreView, NeoSystem.Settings, tokenHash);

            // Scale amount based on decimals
            var scaledAmount = new BigDecimal(amount, asset.Decimals);
            var scaledAmountValueStr = scaledAmount.Value.ToString();

            var argAddress = new JObject();
            argAddress["type"] = "Hash160";
            argAddress["value"] = from.ToString();

            var argHandle = new JObject();
            argHandle["type"] = "ByteArray";
            argHandle["value"] = Convert.ToBase64String(handle);

            var argToken = new JObject();
            argToken["type"] = "Hash160";
            argToken["value"] = tokenHash.ToString();

            var argAmount = new JObject();
            argAmount["type"] = "Integer";
            argAmount["value"] = scaledAmountValueStr;
            
            // Invoke operation
            const string operation = "transferFromAddressToHandle";

            var invokeResult = OnInvokeWithResult(
                slInvokeTokenHash,
                operation, 
                out _, 
                tx, 
                new JArray(argAddress, argHandle, argToken, argAmount), 
                gas: (long)gas.Value
            );

            if (!invokeResult) {
                return;
            }
            
            try {
                tx = CurrentWallet.MakeTransaction(NeoSystem.StoreView, tx.Script, sender, signers, maxGas: (long)gas.Value);
            } catch (InvalidOperationException e) {
                ConsoleHelper.Error($"{GetExceptionMessage(e)}: {e.ToString()}");
                return;
            }

            ConsoleHelper.Info("Network fee: ",
                $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)}\t",
                "Total fee: ",
                $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
            

            if (!ReadUserInput("Relay tx(no|yes)").IsYes()) {
                return;
            }

            SignAndSendTx(NeoSystem.StoreView, tx);
        }

        /// <summary>
        /// Process "balanceOf" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        /// <param name="address">Address</param>
        [ConsoleCommand("balanceOf", Category = "NEP17 Commands")]
        private void OnBalanceOfCommand(UInt160 tokenHash, byte[] handleOrAddress) {
            var handleOrAddressStr = System.Text.Encoding.UTF8.GetString(handleOrAddress);

            // CNR first
            if (Helper.IsCNRAddress(handleOrAddressStr)) {
                var resolved = ResolveAddressByCNR(handleOrAddressStr);
                if (resolved is not null) {
                    // Override to resolved address string to bypass Social Handle flow
                    handleOrAddressStr = resolved.ToString();
                }
            }

            // Handle Social balanceOf
            if (Helper.IsSocialHandle(handleOrAddressStr)) {
                OnBalanceOfHCommand(tokenHash, handleOrAddress);
                return;
            }

            var address = StringToAddress(handleOrAddressStr, NeoSystem.Settings.AddressVersion);
            if (address is null || address == UInt160.Zero) {
                throw new ArgumentException(nameof(handleOrAddress));
            }

            var arg = new JObject();
            arg["type"] = "Hash160";
            arg["value"] = address.ToString();

            var asset = new AssetDescriptor(NeoSystem.StoreView, NeoSystem.Settings, tokenHash);

            if (!OnInvokeWithResult(tokenHash, "balanceOf", out StackItem balanceResult, null, new JArray(arg))) {
                return;
            }

            var balance = new BigDecimal(((PrimitiveType)balanceResult).GetInteger(), asset.Decimals);

            Console.WriteLine();
            ConsoleHelper.Info($"{asset.AssetName} balance: ", $"{balance}");
        }

        /// <summary>
        /// Process "balanceOfh" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        /// <param name="handle">Address</param>
        [ConsoleCommand("balanceOfh", Category = "NEP17 Commands")]
        private void OnBalanceOfHCommand(UInt160 tokenHash, byte[] handle) {
            var argTokenHash = new JObject();
            argTokenHash["type"] = "Hash160";
            argTokenHash["value"] = tokenHash.ToString();

            var argHandle = new JObject();
            argHandle["type"] = "ByteArray";
            argHandle["value"] = Convert.ToBase64String(handle);

            var asset = new AssetDescriptor(NeoSystem.StoreView, NeoSystem.Settings, tokenHash);

            if (!OnInvokeWithResult(slInvokeTokenHash, "balanceOf", out StackItem balanceResult, null, new JArray(argHandle, argTokenHash))) {
                return;
            }

            var balance = new BigDecimal(((PrimitiveType)balanceResult).GetInteger(), asset.Decimals);

            Console.WriteLine();
            ConsoleHelper.Info($"{asset.AssetName} balance: ", $"{balance}");
        }

        /// <summary>
        /// Process "name" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        [ConsoleCommand("name", Category = "NEP17 Commands")]
        private void OnNameCommand(UInt160 tokenHash)
        {
            ContractState contract = NativeContract.ContractManagement.GetContract(NeoSystem.StoreView, tokenHash);
            if (contract == null) Console.WriteLine($"Contract hash not exist: {tokenHash}");
            else ConsoleHelper.Info("Result: ", contract.Manifest.Name);
        }

        /// <summary>
        /// Process "decimals" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        [ConsoleCommand("decimals", Category = "NEP17 Commands")]
        private void OnDecimalsCommand(UInt160 tokenHash)
        {
            if (!OnInvokeWithResult(tokenHash, "decimals", out StackItem result, null)) return;

            ConsoleHelper.Info("Result: ", $"{((PrimitiveType)result).GetInteger()}");
        }

        /// <summary>
        /// Process "totalSupply" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        [ConsoleCommand("totalSupply", Category = "NEP17 Commands")]
        private void OnTotalSupplyCommand(UInt160 tokenHash)
        {
            if (!OnInvokeWithResult(tokenHash, "totalSupply", out StackItem result, null)) return;

            var asset = new AssetDescriptor(NeoSystem.StoreView, NeoSystem.Settings, tokenHash);
            var totalSupply = new BigDecimal(((PrimitiveType)result).GetInteger(), asset.Decimals);

            ConsoleHelper.Info("Result: ", $"{totalSupply}");
        }
    }
}
