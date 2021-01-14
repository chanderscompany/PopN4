//-----------------------------------------------------------------------
// <copyright file="SpinAPIException.cs" company="SpinCore Technologies, Inc">
//     Copyright (c) SpinCore Technologies, Inc.
// </copyright>
//-----------------------------------------------------------------------

/* Copyright (c) SpinCore Technologies, Inc.
 *
 * This software is provided 'as-is', without any express or implied warranty. 
 * In no event will the authors be held liable for any damages arising from the 
 * use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose, 
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be appreciated
 * but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

namespace SpinCore.SpinAPI
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Spin API Exception
    /// </summary>
    [Serializable]
    public class SpinAPIException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the SpinAPIException class.
        /// </summary>
        public SpinAPIException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SpinAPIException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SpinAPIException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SpinAPIException class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SpinAPIException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SpinAPIException class with the serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SpinAPIException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
