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
using System.Text.RegularExpressions;

namespace EdCanHack.XnaContent
{
    /// <summary>
    /// Used to seed a ContentEngine with default type mappings. For example, to create an equivalent
    /// of the standard XNA texture processor, you can pass in a regex of ".*\.(png|jpeg|jpg|tga)",
    /// an importer of null, and a processor of typeof(TextureProcessor).
    /// </summary>
    public class TypeMapping
    {
        public readonly Regex FileMatcher;
        public readonly Type ImporterType;
        public readonly Type ProcessorType;

        public TypeMapping(String fileMatcherRegex, Type importerType, Type processorType)
            :this(new Regex(fileMatcherRegex), importerType, processorType)
        {
            
        }

        public TypeMapping(Regex fileMatcher, Type importerType, Type processorType)
        {
            FileMatcher = fileMatcher;
            ImporterType = importerType;
            ProcessorType = processorType;
        }
    }
}
