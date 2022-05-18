// Copyright (C) 2016-2021 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.RegularExpressions;

namespace Neo.CLI
{
    internal static class Helper
    {
        public static bool IsYes(this string input)
        {
            if (input == null) return false;

            input = input.ToLowerInvariant();

            return input == "yes" || input == "y";
        }

        public static string ToBase64String(this byte[] input) => System.Convert.ToBase64String(input);

        public static bool IsCNRAddress(string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                return false;
            }

            bool isCNRAddress = 
                Regex.Match(input, @"^[a-zA-Z0-9_\.-]*\.(id.dvita.com)$").Success ||
                input.StartsWith('@') ||
                (input.Contains('@') && input.Contains('.'));
            
            return isCNRAddress;
        }

        public static bool IsSocialHandle(string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                return false;
            }

            // TODO: Single place to share with CLI<->RPC
            // TODO: To extend with D/I for all social providers
            bool isSocialHandle = input.StartsWith('@');
            
            return isSocialHandle;
        }
    }
}
