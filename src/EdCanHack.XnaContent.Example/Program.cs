#region File Description
// -------------------------------------------------------------------------------------------------
// Copyright (C) 2012 Ed Ropple <ed+xnacontent@edropple.com>
//
// Derivative of works Copyright (C) Microsoft Corporation.
// Original project: http://xbox.create.msdn.com/en-US/education/catalog/sample/winforms_series_2
//
// Released under the Microsoft Public License: http://opensource.org/licenses/MS-PL
// -------------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EdCanHack.XnaContent.Example
{
    /// <summary>
    /// This is a super-simple example runner for the ContentEngine. It doesn't
    /// exploit any of the more advanced features out-of-the-box, just a simple
    /// build of a couple of files in the associated Content project.
    /// 
    /// This relies on some hardcoded paths, so this won't work if you move it
    /// out of the solution structure.
    /// </summary>
    static class Program
    {
        private static readonly String[] Files = new String[]
        {
            "orange.png",
            "Special/green.png"
        };

        static void Main(string[] args)
        {
            String appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            WriteLine(ConsoleColor.Yellow, "App running in: {0}", appDirectory);

            String contentDirectory = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "Content"));
            WriteLine(ConsoleColor.Yellow, "Content in: {0}", contentDirectory);

            WriteLine();
            WriteLine();

            WriteLine(ConsoleColor.Yellow, "Constructing ContentEngine.");
            ContentEngine engine = new ContentEngine(new String[0], new TypeMapping[0]);

            WriteLine();
            foreach(String file in Files)
            {
                WriteLine(ConsoleColor.White, "Adding file: {0}", file);
                engine.Add(Path.Combine(contentDirectory, file));
            }

            WriteLine();

            for (Int32 i = 0; i < 3; ++i)
            {
                WriteLine(ConsoleColor.Cyan, "Engine pass #{0} (will hang window for a while)...", i);
                engine.Build();
            }

            WriteLine(ConsoleColor.Cyan, "Done!");

            WriteLine(ConsoleColor.Green, "All done! Press Enter to continue (will open the compiled output directory).");
            Console.ReadLine();

            Process.Start(engine.OutputDirectory);
        }

        static void WriteLine() { Console.WriteLine();}
        static void WriteLine(String text, params Object[] o)
        {
            Console.ResetColor();
            Console.WriteLine(text, o);
        }
        static void WriteLine(ConsoleColor color, String text, params Object[] o)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text, o);
        }
    }
}
