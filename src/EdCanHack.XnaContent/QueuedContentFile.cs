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

namespace EdCanHack.XnaContent
{
    public class QueuedContentFile
    {
        public readonly String Filename;
        public readonly String ContentName;
        public readonly Type ImporterType;
        public readonly Type ProcessorType;

        public QueuedContentFile(String filename, String contentName, 
                                 Type importerType, Type processorType)
        {
            Filename = filename;
            ContentName = contentName;
            ImporterType = importerType;
            ProcessorType = processorType;
        }
    }
}
