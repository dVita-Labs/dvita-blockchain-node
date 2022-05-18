// Copyright (C) 2016-2021 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using Neo.ConsoleService;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;

namespace Neo.CLI {
    partial class MainService {
        // Resolver deployed
        // 0x297801f069dd9fa340f8abb7274b2ae40ff8466b -> NVhCWHzmB4pRKsLzaBSyU4uxddgsvUsX9V
        private const string scriptHashStr = "NVhCWHzmB4pRKsLzaBSyU4uxddgsvUsX9V";

        internal static UInt160 ResolveAddressByCNR(string cname) {
            try {
                using(var scriptBuilder = new ScriptBuilder()) {
                    var scriptHash = scriptHashStr.ToScriptHash(instanceRef.NeoSystem.Settings.AddressVersion);
                    scriptBuilder.EmitDynamicCall(scriptHash, "resolve", cname);
                    var script = scriptBuilder.ToArray();
                    using(var engine = ApplicationEngine.Run(script, instanceRef.NeoSystem.StoreView, container: null, settings: instanceRef.NeoSystem.Settings, gas: TestModeGas)) {
                        var result = engine.State == VMState.FAULT ? null : engine.ResultStack.Peek();
                        if (result is not null && !result.IsNull) {
                            var resultSpan = ((ByteString)result).GetSpan();
                            var resultScriptHash = new UInt160(resultSpan);
                            Console.WriteLine($"CNR for: {cname} is RESULT: {result.Type} {resultScriptHash}");
                            return resultScriptHash;
                        } else {
                            Console.Error.WriteLine($"CNR for: {cname} wrong result: {result}, state: {engine.State}");
                        }
                    }
                }
            } catch (Exception e) {
                Console.Error.WriteLine($"Error: {e.Message}: {e.StackTrace}.");
            }

            Console.Error.WriteLine($"CNR for: {cname} failed.");

            return null;
        }

        internal static bool RegisterAddressByCNR(string cname, UInt160 address, UInt160 signer) {
            Console.WriteLine($"CNR registration for: {cname} -> {address}");

            try {
                using(var scriptBuilder = new ScriptBuilder()) {
                    var scriptHash = scriptHashStr.ToScriptHash(instanceRef.NeoSystem.Settings.AddressVersion);
                    scriptBuilder.EmitDynamicCall(scriptHash, "register", cname, address);
                    var script = scriptBuilder.ToArray();
                    instanceRef.SendTransaction(script, signer, (long)TestModeGas);
                    return true;
                }
            } catch (Exception e) {
                Console.Error.WriteLine($"Error: {e.Message}: {e.StackTrace}.");
            }

            Console.Error.WriteLine($"CNR registration for: {cname} -> {address} failed.");

            return false;
        }

        internal static bool UnregisterAddressByCNR(string cname, UInt160 signer) {
            Console.WriteLine($"CNR unregistration for: {cname}");
            
            try {
                using(var scriptBuilder = new ScriptBuilder()) {
                    var scriptHash = scriptHashStr.ToScriptHash(instanceRef.NeoSystem.Settings.AddressVersion);
                    scriptBuilder.EmitDynamicCall(scriptHash, "unregister", cname);
                    var script = scriptBuilder.ToArray();
                    instanceRef.SendTransaction(script, signer, (long)TestModeGas);
                    return true;
                }
            } catch (Exception e) {
                Console.Error.WriteLine($"Error: {e.Message}: {e.StackTrace}.");
            }

            Console.Error.WriteLine($"CNR unregistration for: {cname} failed.");

            return false;
        }

        /// <summary>
        /// Process "resolve" command
        /// </summary>
        [ConsoleCommand("resolve", Category = "CNR Commands", Description = "Resolves string input to according registered address")]
        private void OnResolveCommand(string value)
        {
            if (Helper.IsCNRAddress(value))
            {
                var resolved = ResolveAddressByCNR(value);
                var address = resolved?.ToAddress(NeoSystem.Settings.AddressVersion) ?? "<unknown address>";
                Console.WriteLine($"{address}");
            }
            else
            {
                ConsoleHelper.Error("Input value is invalid - it should be either email or alphanumeric value ending with .id.dvita.com");
            }
        }

        /// <summary>
        /// Process "register" command
        /// </summary>
        /// <param name="cname">CNR name</param>
        /// <param name="address">Address</param>
        /// <param name="signer">Signer</param>
        [ConsoleCommand("register", Category = "CNR Commands")]
        private void OnRegisterCommand(string cname, UInt160 address, UInt160 signer)
        {
            if (Helper.IsCNRAddress(cname))
            {
                var success = RegisterAddressByCNR(cname, address, signer);
                if (!success)
                {
                    Console.Error.WriteLine("Failed to register address");
                }
            }
            else
            {
                ConsoleHelper.Error("CNR name input is invalid - it should be either email or alphanumeric value ending with .id.dvita.com");
            }
        }

        /// <summary>
        /// Process "unregister" command
        /// </summary>
        /// <param name="cname">CNR name</param>
        /// <param name="signer">Signer</param>
        [ConsoleCommand("unregister", Category = "CNR Commands")]
        private void OnUnregisterCommand(string cname, UInt160 signer)
        {
            if (Helper.IsCNRAddress(cname))
            {
                var success = UnregisterAddressByCNR(cname, signer);
                if (!success)
                {
                    Console.Error.WriteLine("Failed to unregister address");
                }
            }
            else
            {
                ConsoleHelper.Error("CNR name input is invalid - it should be either email or alphanumeric value ending with .id.dvita.com");
            }
        }
    }
}
