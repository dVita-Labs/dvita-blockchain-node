// Copyright (C) 2016-2021 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI;

namespace Neo
{
    static class Program
    {
        static void Main(string[] args)
        {
            var mainService = new MainService();
            mainService.Run(args);
        }
    }
}