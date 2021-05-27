﻿/*
Copyright (C) 2020 DeathCradle

This file is part of Open Terraria API v3 (OTAPI)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using System;

namespace Terraria
{
    partial class patch_Program
    {
#if !Terraria
        public static extern void orig_DisplayException(Exception e);
        public static void DisplayException(Exception e)
        {
            Console.WriteLine(e.ToString());
        }
#endif

#if !TerrariaServer
        public static event EventHandler OnLaunched;
        public static extern void orig_LaunchGame(string[] args, bool monoArgs = false);
        public static void LaunchGame(string[] args, bool monoArgs = false)
        {
            //PluginLoader.TryLoad();
            Console.WriteLine($"[OTAPI] Starting up.");
            //Modifier.Apply(ModType.Runtime);

            Main.versionNumber += " OTAPI";
            Main.versionNumber2 += " OTAPI";

            OnLaunched?.Invoke(null, EventArgs.Empty);

            orig_LaunchGame(args, monoArgs);
        }
#endif
    }
}