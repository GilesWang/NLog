// 
// Copyright (c) 2004-2009 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace NLog.LayoutRenderers
{
    /// <summary>
    /// A counter value (increases on each layout rendering).
    /// </summary>
    [LayoutRenderer("counter")]
    public class CounterLayoutRenderer : LayoutRenderer
    {
        private static Dictionary<string, int> sequences = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the CounterLayoutRenderer class.
        /// </summary>
        public CounterLayoutRenderer()
        {
            this.Increment = 1;
            this.Value = 1;
        }

        /// <summary>
        /// Gets or sets the initial value of the counter.
        /// </summary>
        [DefaultValue(1)]
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the value to be added to the counter after each layout rendering.
        /// </summary>
        [DefaultValue(1)]
        public int Increment { get; set; }

        /// <summary>
        /// Gets or sets the name of the sequence. Different named sequences can have individual values.
        /// </summary>
        public string Sequence { get; set; }

        /// <summary>
        /// Returns the estimated number of characters that are needed to
        /// hold the rendered value for the specified logging event.
        /// </summary>
        /// <param name="logEvent">Logging event information.</param>
        /// <returns>The number of characters.</returns>
        /// <remarks>
        /// If the exact number is not known or
        /// expensive to calculate this function should return a rough estimate
        /// that's big enough in most cases, but not too big, in order to conserve memory.
        /// </remarks>
        protected internal override int GetEstimatedBufferSize(LogEventInfo logEvent)
        {
            return 32;
        }

        /// <summary>
        /// Renders the specified counter value and appends it to the specified <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the rendered data to.</param>
        /// <param name="logEvent">Logging event.</param>
        protected internal override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            int v;

            if (this.Sequence != null)
            {
                v = GetNextSequenceValue(this.Sequence, this.Value, this.Increment);
            }
            else
            {
                v = this.Value;
                this.Value += this.Increment;
            }

            builder.Append(v.ToString(CultureInfo.InvariantCulture));
        }

        private static int GetNextSequenceValue(string sequenceName, int defaultValue, int increment)
        {
            lock (sequences)
            {
                int val;

                if (!sequences.TryGetValue(sequenceName, out val))
                {
                    val = defaultValue;
                }

                int retVal = val;

                val += increment;
                sequences[sequenceName] = val;
                return retVal;
            }
        }
    }
}
