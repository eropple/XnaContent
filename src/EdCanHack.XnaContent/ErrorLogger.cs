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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Framework;

#endregion

namespace EdCanHack.XnaContent
{
    /// <summary>
    /// Custom implementation of the MSBuild ILogger interface records
    /// content build errors so we can later display them to the user. It also dumps
    /// them out to the debug stream when built with DEBUG.
    /// </summary>
    class ErrorLogger : ILogger
    {
        /// <summary>
        /// Initializes the custom logger, hooking the ErrorRaised notification event.
        /// </summary>
        public void Initialize(IEventSource eventSource)
        {
            if (eventSource != null)
            {
                eventSource.ErrorRaised += ErrorRaised;
            }
        }


        /// <summary>
        /// Shuts down the custom logger.
        /// </summary>
        public void Shutdown()
        {
        }


        /// <summary>
        /// Handles error notification events by storing the error message String.
        /// </summary>
        void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            _errors.Add(e.Message);
#if DEBUG
            Debug.WriteLine("CONTENT: " + e.Message);
#endif
        }


        /// <summary>
        /// Gets a list of all the errors that have been logged.
        /// </summary>
        public List<String> Errors
        {
            get { return _errors; }
        }

        readonly List<String> _errors = new List<String>();


        #region ILogger Members


        /// <summary>
        /// Implement the ILogger.Parameters property.
        /// </summary>
        String ILogger.Parameters { get; set; }


        /// <summary>
        /// Implement the ILogger.Verbosity property.
        /// </summary>
        LoggerVerbosity ILogger.Verbosity { get; set; }


        #endregion
    }
}
