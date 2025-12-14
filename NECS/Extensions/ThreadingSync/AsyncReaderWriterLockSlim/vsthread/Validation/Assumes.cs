// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace VSThread.Validation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Runtime;

    /// <summary>
    /// Common runtime checks that throw <see cref="InternalErrorException" /> exceptions upon failure.
    /// </summary>
    public static partial class Assumes
    {
        /// <summary>
        /// Throws <see cref="InternalErrorException" /> if the specified value is null.
        /// </summary>
        /// <typeparam name="T">The type of value to test.</typeparam>
        [DebuggerStepThrough]
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
        public static void NotNull<T>([ValidatedNotNull, NotNull]T value)
            where T : class
        {
            True(value is object);
        }

        /// <summary>
        /// Throws <see cref="InternalErrorException" /> if the specified value is not null.
        /// </summary>
        /// <typeparam name="T">The type of value to test.</typeparam>
        [DebuggerStepThrough]
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
        public static void Null<T>(T value)
            where T : class
        {
            True(value is null);
        }

        /// <summary>
        /// Throws an public exception if a condition evaluates to true.
        /// </summary>
        [DebuggerStepThrough]
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
        public static void False([DoesNotReturnIf(true)] bool condition, [Localizable(false)] string message = null)
        {
            if (condition)
            {
                Fail(message);
            }
        }

        /// <summary>
        /// Throws an public exception if a condition evaluates to false.
        /// </summary>
        [DebuggerStepThrough]
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
        public static void True([DoesNotReturnIf(false)] bool condition, [Localizable(false)] string message = null)
        {
            if (!condition)
            {
                Fail(message);
            }
        }

        /// <summary>
        /// Unconditionally throws an <see cref="InternalErrorException"/>.
        /// </summary>
        /// <returns>Nothing. This method always throws.</returns>
        [DebuggerStepThrough]
        public static Exception NotReachable()
        {
            // Keep these two as separate lines of code, so the debugger can come in during the assert dialog
            // that the exception's constructor displays, and the debugger can then be made to skip the throw
            // in order to continue the investigation.
            var exception = new InternalErrorException();
            bool proceed = true; // allows debuggers to skip the throw statement
            if (proceed)
            {
                throw exception;
            }
            else
            {
#pragma warning disable CS8763
                return new Exception();
#pragma warning restore CS8763
            }
        }

        /// <summary>
        /// Throws an public exception.
        /// </summary>
        /// <returns>Nothing, as this method always throws.  The signature allows for "throwing" Fail so C# knows execution will stop.</returns>
        [DebuggerStepThrough]
        public static Exception Fail([Localizable(false)] string message = null)
        {
            var exception = new InternalErrorException(message);
            bool proceed = true; // allows debuggers to skip the throw statement
            if (proceed)
            {
                throw exception;
            }
            else
            {
#pragma warning disable CS8763
                return new Exception();
#pragma warning restore CS8763
            }
        }
    }
}
