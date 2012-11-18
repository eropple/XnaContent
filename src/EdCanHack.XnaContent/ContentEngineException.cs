#region File Description
// -------------------------------------------------------------------------------------------------
// Copyright (C) 2012 Ed Ropple <ed+xnacontent@edropple.com>
//
// Derivative of works Copyright (C) Microsoft Corporation.
// Original project: http://xbox.create.msdn.com/en-US/education/catalog/sample/winforms_series_1
//
// Released under the Microsoft Public License: http://opensource.org/licenses/MS-PL
// -------------------------------------------------------------------------------------------------
#endregion

using System;

namespace EdCanHack.XnaContent
{
    public class ContentEngineException : Exception
    {
        public ContentEngineException()
        {
        }

        public ContentEngineException(String message) : base(message)
        {
        }
        
        public ContentEngineException(String message, params Object[] o) : base(String.Format(message, o))
        {
        }

        public ContentEngineException(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
