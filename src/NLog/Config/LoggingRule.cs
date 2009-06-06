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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NLog.Filters;
using NLog.Targets;

namespace NLog.Config
{
    /// <summary>
    /// Represents a logging rule. An equivalent of &lt;logger /&gt; configuration element.
    /// </summary>
    public class LoggingRule
    {
        private string loggerNamePattern;
        private MatchMode loggerNameMatchMode;
        private string loggerNameMatchArgument;

        private bool[] logLevels = new bool[LogLevel.MaxLevel.Ordinal + 1];
        private ICollection<Target> targets = new List<Target>();
        private IList<Filter> filters = new List<Filter>();
        private ICollection<LoggingRule> childRules = new List<LoggingRule>();

        /// <summary>
        /// Initializes a new instance of the LoggingRule class.
        /// </summary>
        public LoggingRule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the LoggingRule class.
        /// </summary>
        /// <param name="loggerNamePattern">Logger name pattern. It may include the '*' wildcard at the beginning, at the end or at both ends.</param>
        /// <param name="minLevel">Minimum log level needed to trigger this rule.</param>
        /// <param name="target">Target to be written to when the rule matches.</param>
        public LoggingRule(string loggerNamePattern, LogLevel minLevel, Target target)
        {
            this.LoggerNamePattern = loggerNamePattern;
            this.targets.Add(target);
            for (int i = (int)minLevel.Ordinal; i <= (int)LogLevel.MaxLevel.Ordinal; ++i)
            {
                this.EnableLoggingForLevel(LogLevel.FromOrdinal(i));
            }
        }

        /// <summary>
        /// Initializes a new instance of the LoggingRule class.
        /// </summary>
        /// <param name="loggerNamePattern">Logger name pattern. It may include the '*' wildcard at the beginning, at the end or at both ends.</param>
        /// <param name="target">Target to be written to when the rule matches.</param>
        /// <remarks>By default no logging levels are defined. You should call <see cref="EnableLoggingForLevel"/> and <see cref="DisableLoggingForLevel"/> to set them.</remarks>
        public LoggingRule(string loggerNamePattern, Target target)
        {
            this.LoggerNamePattern = loggerNamePattern;
            this.targets.Add(target);
        }

        internal enum MatchMode
        {
            All,
            None,
            Equals,
            StartsWith,
            EndsWith,
            Contains,
        }

        /// <summary>
        /// Gets a collection of targets that should be written to when this rule matches.
        /// </summary>
        public ICollection<Target> Targets
        {
            get { return this.targets; }
        }

        /// <summary>
        /// Gets a collection of child rules to be evaluated when this rule matches.
        /// </summary>
        public ICollection<LoggingRule> ChildRules
        {
            get { return this.childRules; }
        }

        /// <summary>
        /// Gets a collection of filters to be checked before writing to targets.
        /// </summary>
        public IList<Filter> Filters
        {
            get { return this.filters; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to quit processing any further rule when this one matches.
        /// </summary>
        public bool Final { get; set; }

        /// <summary>
        /// Gets or sets logger name pattern.
        /// </summary>
        /// <remarks>
        /// Logger name pattern. It may include the '*' wildcard at the beginning, at the end or at both ends but not anywhere else.
        /// </remarks>
        public string LoggerNamePattern
        {
            get
            {
                return this.loggerNamePattern;
            }

            set
            {
                this.loggerNamePattern = value;
                int firstPos = this.loggerNamePattern.IndexOf('*');
                int lastPos = this.loggerNamePattern.LastIndexOf('*');

                if (firstPos < 0)
                {
                    this.loggerNameMatchMode = MatchMode.Equals;
                    this.loggerNameMatchArgument = value;
                    return;
                }

                if (firstPos == lastPos)
                {
                    string before = this.LoggerNamePattern.Substring(0, firstPos);
                    string after = this.LoggerNamePattern.Substring(firstPos + 1);

                    if (before.Length > 0)
                    {
                        this.loggerNameMatchMode = MatchMode.StartsWith;
                        this.loggerNameMatchArgument = before;
                        return;
                    }

                    if (after.Length > 0)
                    {
                        this.loggerNameMatchMode = MatchMode.EndsWith;
                        this.loggerNameMatchArgument = after;
                        return;
                    }

                    return;
                }

                // *text*
                if (firstPos == 0 && lastPos == this.LoggerNamePattern.Length - 1)
                {
                    string text = this.LoggerNamePattern.Substring(1, this.LoggerNamePattern.Length - 2);
                    this.loggerNameMatchMode = MatchMode.Contains;
                    this.loggerNameMatchArgument = text;
                    return;
                }

                this.loggerNameMatchMode = MatchMode.None;
                this.loggerNameMatchArgument = String.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the collection of log levels enabled by this rule.
        /// </summary>
        /// <value>The log levels.</value>
        public ICollection<LogLevel> Levels
        {
            get
            {
                List<LogLevel> levels = new List<LogLevel>();

                for (int i = 0; i < LogLevel.MaxLevel.Ordinal; ++i)
                {
                    if (this.logLevels[i])
                    {
                        levels.Add(LogLevel.FromOrdinal(i));
                    }
                }

                return levels.AsReadOnly();
            }

            set
            {
                for (int i = 0; i < this.logLevels.Length; ++i)
                {
                    this.logLevels[i] = false;
                }

                foreach (LogLevel ll in value)
                {
                    this.logLevels[ll.Ordinal] = true;
                }
            }
        }

        /// <summary>
        /// Enables logging for a particular level.
        /// </summary>
        /// <param name="level">Level to be enabled.</param>
        public void EnableLoggingForLevel(LogLevel level)
        {
            this.logLevels[level.Ordinal] = true;
        }

        /// <summary>
        /// Disables logging for a particular level.
        /// </summary>
        /// <param name="level">Level to be disabled.</param>
        public void DisableLoggingForLevel(LogLevel level)
        {
            this.logLevels[level.Ordinal] = false;
        }

        /// <summary>
        /// Returns a string representation of <see cref="LoggingRule"/>. Used for debugging.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(CultureInfo.InvariantCulture, "logNamePattern: ({0}:{1})", this.loggerNameMatchArgument, this.loggerNameMatchMode);
            sb.Append(" levels: [ ");
            for (int i = 0; i < this.logLevels.Length; ++i)
            {
                if (this.logLevels[0])
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", LogLevel.FromOrdinal(i).ToString());
                }
            }

            sb.Append("] appendTo: [ ");
            foreach (Target app in this.targets)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", app.Name);
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Checks whether te particular log level is enabled for this rule.
        /// </summary>
        /// <param name="level">Level to be checked.</param>
        /// <returns>A value of <see langword="true"/> when the log level is enabled, <see langword="false" /> otherwise.</returns>
        public bool IsLoggingEnabledForLevel(LogLevel level)
        {
            return this.logLevels[level.Ordinal];
        }

        /// <summary>
        /// Checks whether given name matches the logger name pattern.
        /// </summary>
        /// <param name="loggerName">String to be matched.</param>
        /// <returns>A value of <see langword="true"/> when the name matches, <see langword="false" /> otherwise.</returns>
        public bool NameMatches(string loggerName)
        {
            switch (this.loggerNameMatchMode)
            {
                case MatchMode.All:
                    return true;

                default:
                case MatchMode.None:
                    return false;

                case MatchMode.Equals:
                    return String.CompareOrdinal(loggerName, this.loggerNameMatchArgument) == 0;

                case MatchMode.StartsWith:
                    return loggerName.StartsWith(this.loggerNameMatchArgument);

                case MatchMode.EndsWith:
                    return loggerName.EndsWith(this.loggerNameMatchArgument);

                case MatchMode.Contains:
                    return loggerName.IndexOf(this.loggerNameMatchArgument) >= 0;
            }
        }
    }
}
