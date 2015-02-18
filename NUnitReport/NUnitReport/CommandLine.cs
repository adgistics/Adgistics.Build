﻿#region Header

//
// Command Line Library: CommandLine.cs
//
// Author:
//   Giacomo Stelluti Scala (gsscoder@gmail.com)
//
// Copyright (C) 2005 - 2012 Giacomo Stelluti Scala
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

#endregion Header

namespace CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Threading;

    #region Enumerations

    [Flags]
    public enum ParserState : ushort
    {
        Success             = 0x01,
        Failure             = 0x02,
        MoveOnNextElement   = 0x04
    }

    #endregion Enumerations

    public interface IArgumentEnumerator : IDisposable
    {
        #region Properties

        string Current
        {
            get;
        }

        bool IsLast
        {
            get;
        }

        string Next
        {
            get;
        }

        #endregion Properties

        #region Methods

        string GetRemainingFromNext();

        bool MoveNext();

        bool MovePrevious();

        #endregion Methods
    }

    /// <summary>
    /// Defines a basic interface to parse command line arguments.
    /// </summary>
    public interface ICommandLineParser
    {
        #region Methods

        /// <summary>
        /// Parses a String array of command line arguments, setting values in <paramref name="options"/>
        /// parameter instance's public fields decorated with appropriate attributes.
        /// </summary>
        /// <param name="args">A String array of command line arguments.</param>
        /// <param name="options">An object's instance used to receive values.
        /// Parsing rules are defined using <see cref="CommandLine.BaseOptionAttribute"/> derived types.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        bool ParseArguments(string[] args, object options);

        /// <summary>
        /// Parses a String array of command line arguments, setting values in <paramref name="options"/>
        /// parameter instance's public fields decorated with appropriate attributes.
        /// This overload allows you to specify a <see cref="System.IO.TextWriter"/> derived instance for write text messages.         
        /// </summary>
        /// <param name="args">A String array of command line arguments.</param>
        /// <param name="options">An object's instance used to receive values.
        /// Parsing rules are defined using <see cref="CommandLine.BaseOptionAttribute"/> derived types.</param>
        /// <param name="helpWriter">Any instance derived from <see cref="System.IO.TextWriter"/>,
        /// usually <see cref="System.Console.Error"/>. Setting this argument to null, will disable help screen.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        bool ParseArguments(string[] args, object options, TextWriter helpWriter);

        #endregion Methods
    }

    public abstract class ArgumentParser
    {
        #region Constructors

        public ArgumentParser()
        {
            PostParsingState = new List<ParsingError>();
        }

        #endregion Constructors

        #region Properties

        public List<ParsingError> PostParsingState
        {
            get; private set;
        }

        #endregion Properties

        #region Methods

        public static ParserState BooleanToParserState(bool value)
        {
            return BooleanToParserState(value, false);
        }

        public static ParserState BooleanToParserState(bool value, bool addMoveNextIfTrue)
        {
            if (value && !addMoveNextIfTrue)
            {
                return ParserState.Success;
            }

            if (value)
            {
                return ParserState.Success | ParserState.MoveOnNextElement;
            }

            return ParserState.Failure;
        }

        public static bool CompareLong(string argument, string option, bool caseSensitive)
        {
            return string.Compare(argument, "--" + option, !caseSensitive) == 0;
        }

        public static bool CompareShort(string argument, string option, bool caseSensitive)
        {
            return string.Compare(argument, "-" + option, !caseSensitive) == 0;
        }

        public static ArgumentParser Create(string argument, bool ignoreUnknownArguments = false)
        {
            if (argument.Equals("-", StringComparison.InvariantCulture))
            {
                return null;
            }

            if (argument[0] == '-' && argument[1] == '-')
            {
                return new LongOptionParser(ignoreUnknownArguments);
            }

            if (argument[0] == '-')
            {
                return new OptionGroupParser(ignoreUnknownArguments);
            }

            return null;
        }

        public static void EnsureOptionArrayAttributeIsNotBoundToScalar(OptionInfo option)
        {
            if (!option.IsArray && option.IsAttributeArrayCompatible)
            {
                throw new CommandLineParserException();
            }
        }

        public static void EnsureOptionAttributeIsArrayCompatible(OptionInfo option)
        {
            if (!option.IsAttributeArrayCompatible)
            {
                throw new CommandLineParserException();
            }
        }

        public static IList<string> GetNextInputValues(IArgumentEnumerator ae)
        {
            IList<string> list = new List<string>();

            while (ae.MoveNext())
            {
                if (IsInputValue(ae.Current))
                {
                    list.Add(ae.Current);
                } else
                {
                    break;
                }
            }
            if (!ae.MovePrevious())
            {
                throw new CommandLineParserException();
            }

            return list;
        }

        public static bool IsInputValue(string argument)
        {
            if (argument.Length > 0)
            {
                return argument.Equals("-", StringComparison.InvariantCulture) || argument[0] != '-';
            }

            return true;
        }

        public void DefineOptionThatViolatesFormat(OptionInfo option)
        {
            PostParsingState.Add(new ParsingError(option.ShortName, option.LongName, true));
        }

        public abstract ParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options);

        #endregion Methods

        #if UNIT_TESTS

        public static IList<string> PublicWrapperOfGetNextInputValues(IArgumentEnumerator ae)
        {
            return GetNextInputValues(ae);
        }

        #endif
    }

    public static class Assumes
    {
        #region Methods

        public static void NotNull<T>(T value, string paramName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void NotNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(paramName);
            }
        }

        public static void NotZeroLength<T>(T[] array, string paramName)
        {
            if (array.Length == 0)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        #endregion Methods
    }

    /// <summary>
    /// Models a bad parsed option.
    /// </summary>
    public sealed class BadOptionInfo
    {
        #region Constructors

        public BadOptionInfo()
        {
        }

        public BadOptionInfo(string shortName, string longName)
        {
            ShortName = shortName;
            LongName = longName;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The long name of the option
        /// </summary>
        /// <value>Returns the long name of the option.</value>
        public string LongName
        {
            get;
            set;
        }

        /// <summary>
        /// The short name of the option
        /// </summary>
        /// <value>Returns the short name of the option.</value>
        public string ShortName
        {
            get;
            set;
        }

        #endregion Properties
    }

    /// <summary>
    /// Provides base properties for creating an attribute, used to define rules for command line parsing.
    /// </summary>
    public abstract class BaseOptionAttribute : Attribute
    {
        #region Fields

        private object _defaultValue;
        private bool _hasDefaultValue;
        private string _shortName;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets mapped property default value.
        /// </summary>
        public object DefaultValue
        {
            get { return _defaultValue; }
            set {
                _defaultValue = value;
                _hasDefaultValue = true;
            }
        }

        public bool HasDefaultValue
        {
            get { return _hasDefaultValue; }
        }

        public bool HasLongName
        {
            get { return !string.IsNullOrEmpty(LongName); }
        }

        public bool HasShortName
        {
            get { return !string.IsNullOrEmpty(_shortName); }
        }

        /// <summary>
        /// A short description of this command line option. Usually a sentence summary. 
        /// </summary>
        public string HelpText
        {
            get; set;
        }

        /// <summary>
        /// Long name of this command line option. This name is usually a single english word.
        /// </summary>
        public string LongName
        {
            get; set;
        }

        /// <summary>
        /// True if this command line option is required.
        /// </summary>
        public virtual bool Required
        {
            get; set;
        }

        /// <summary>
        /// Short name of this command line option. You can use only one character.
        /// </summary>
        public string ShortName
        {
            get { return _shortName; }
            set {
                if (value != null && value.Length > 1)
                {
                    throw new ArgumentException("shortName");
                }

                _shortName = value;
            }
        }

        #endregion Properties
    }

    /// <summary>
    ///   Provides the abstract base class for a strongly typed options target. 
    ///   Used when you need to get parsing errors.
    /// </summary>
    public abstract class CommandLineOptionsBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="CommandLine.CommandLineOptionsBase"/> class.
        /// </summary>
        public CommandLineOptionsBase()
        {
            LastPostParsingState = new PostParsingState();
        }

        #endregion Constructors

        #region Properties

        public PostParsingState InternalLastPostParsingState
        {
            get { return LastPostParsingState; }
        }

        /// <summary>
        /// Gets the last state of the post parsing.
        /// </summary>
        /// <value>
        /// The last state of the post parsing.
        /// </value>
        public PostParsingState LastPostParsingState
        {
            get; private set;
        }

        #endregion Properties
    }

    /// <summary>
    /// Provides methods to parse command line arguments.
    /// Default implementation for <see cref="CommandLine.ICommandLineParser"/>.
    /// </summary>
    public class CommandLineParser : ICommandLineParser
    {
        #region Fields

        private static readonly ICommandLineParser _default = new CommandLineParser(true);

        private readonly CommandLineParserSettings _settings;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParser"/> class.
        /// </summary>
        public CommandLineParser()
        {
            _settings = new CommandLineParserSettings();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParser"/> class,
        /// configurable with a <see cref="CommandLine.CommandLineParserSettings"/> object.
        /// </summary>
        /// <param name="settings">The <see cref="CommandLine.CommandLineParserSettings"/> object is used to configure
        /// aspects and behaviors of the parser.</param>
        public CommandLineParser(CommandLineParserSettings settings)
        {
            Assumes.NotNull(settings, "settings");

            _settings = settings;
        }

        // special constructor for singleton instance, parameter ignored
        private CommandLineParser(bool singleton)
        {
            _settings = new CommandLineParserSettings(false, false, Console.Error);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Singleton instance created with basic defaults.
        /// </summary>
        public static ICommandLineParser Default
        {
            get { return _default; }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Parses a String array of command line arguments, setting values in <paramref name="options"/>
        /// parameter instance's public fields decorated with appropriate attributes.
        /// </summary>
        /// <param name="args">A String array of command line arguments.</param>
        /// <param name="options">An object's instance used to receive values.
        /// Parsing rules are defined using <see cref="CommandLine.BaseOptionAttribute"/> derived types.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public virtual bool ParseArguments(string[] args, object options)
        {
            return ParseArguments(args, options, _settings.HelpWriter);
        }

        /// <summary>
        /// Parses a String array of command line arguments, setting values in <paramref name="options"/>
        /// parameter instance's public fields decorated with appropriate attributes.
        /// This overload allows you to specify a <see cref="System.IO.TextWriter"/> derived instance for write text messages.         
        /// </summary>
        /// <param name="args">A String array of command line arguments.</param>
        /// <param name="options">An object's instance used to receive values.
        /// Parsing rules are defined using <see cref="CommandLine.BaseOptionAttribute"/> derived types.</param>
        /// <param name="helpWriter">Any instance derived from <see cref="System.IO.TextWriter"/>,
        /// usually <see cref="System.Console.Error"/>. Setting this argument to null, will disable help screen.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public virtual bool ParseArguments(string[] args, object options, TextWriter helpWriter)
        {
            Assumes.NotNull(args, "args");
            Assumes.NotNull(options, "options");

            var pair = ReflectionUtil.RetrieveMethod<HelpOptionAttribute>(options);

            if (pair != null && helpWriter != null)
            {
                if (ParseHelp(args, pair.Right) || !DoParseArguments(args, options))
                {
                    string helpText;
                    HelpOptionAttribute.InvokeMethod(options, pair, out helpText);
                    helpWriter.Write(helpText);
                    return false;
                }
                return true;
            }

            return DoParseArguments(args, options);
        }

        //private static void SetPostParsingStateIfNeeded(object options, PostParsingState state)
        private static void SetPostParsingStateIfNeeded(object options, IEnumerable<ParsingError> state)
        {
            var commandLineOptionsBase = options as CommandLineOptionsBase;
            if (commandLineOptionsBase != null)
            {
                (commandLineOptionsBase).InternalLastPostParsingState.Errors.AddRange(state);
            }
        }

        private bool DoParseArguments(string[] args, object options)
        {
            bool hadError = false;
            var optionMap = OptionInfo.CreateMap(options, _settings);
            optionMap.SetDefaults();
            var target = new TargetWrapper(options);

            IArgumentEnumerator arguments = new StringArrayEnumerator(args);
            while (arguments.MoveNext())
            {
                string argument = arguments.Current;
                if (!string.IsNullOrEmpty(argument))
                {
                    ArgumentParser parser = ArgumentParser.Create(argument, _settings.IgnoreUnknownArguments);
                    if (parser != null)
                    {
                        ParserState result = parser.Parse(arguments, optionMap, options);
                        if ((result & ParserState.Failure) == ParserState.Failure)
                        {
                            SetPostParsingStateIfNeeded(options, parser.PostParsingState);
                            hadError = true;
                            continue;
                        }

                        if ((result & ParserState.MoveOnNextElement) == ParserState.MoveOnNextElement)
                        {
                            arguments.MoveNext();
                        }
                    } else if (target.IsValueListDefined)
                    {
                        if (!target.AddValueItemIfAllowed(argument))
                        {
                            hadError = true;
                        }
                    }
                }

            }

            hadError |= !optionMap.EnforceRules();

            return !hadError;
        }

        private bool ParseHelp(string[] args, HelpOptionAttribute helpOption)
        {
            bool caseSensitive = _settings.CaseSensitive;

            for (int i = 0; i < args.Length; i++)
            {
                if (!string.IsNullOrEmpty(helpOption.ShortName))
                {
                    if (ArgumentParser.CompareShort(args[i], helpOption.ShortName, caseSensitive))
                    {
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(helpOption.LongName))
                {
                    if (ArgumentParser.CompareLong(args[i], helpOption.LongName, caseSensitive))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion Methods
    }

    /// <summary>
    /// This exception is thrown when a generic parsing error occurs.
    /// </summary>
    [Serializable]
    public sealed class CommandLineParserException : Exception
    {
        #region Constructors

        public CommandLineParserException()
        {
        }

        public CommandLineParserException(string message)
            : base(message)
        {
        }

        public CommandLineParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CommandLineParserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion Constructors
    }

    /// <summary>
    /// Specifies a set of features to configure a <see cref="CommandLine.CommandLineParser"/> behavior.
    /// </summary>
    public sealed class CommandLineParserSettings
    {
        #region Fields

        private const bool CASE_SENSITIVE_DEFAULT = true;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class.
        /// </summary>
        public CommandLineParserSettings()
            : this(CASE_SENSITIVE_DEFAULT)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class,
        /// setting the case comparison behavior.
        /// </summary>
        /// <param name="caseSensitive">If set to true, parsing will be case sensitive.</param>
        public CommandLineParserSettings(bool caseSensitive)
        {
            CaseSensitive = caseSensitive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class,
        /// setting the <see cref="System.IO.TextWriter"/> used for help method output.
        /// </summary>
        /// <param name="helpWriter">Any instance derived from <see cref="System.IO.TextWriter"/>,
        /// default <see cref="System.Console.Error"/>. Setting this argument to null, will disable help screen.</param>
        public CommandLineParserSettings(TextWriter helpWriter)
            : this(CASE_SENSITIVE_DEFAULT)
        {
            HelpWriter = helpWriter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class,
        /// setting case comparison and help output options.
        /// </summary>
        /// <param name="caseSensitive">If set to true, parsing will be case sensitive.</param>
        /// <param name="helpWriter">Any instance derived from <see cref="System.IO.TextWriter"/>,
        /// default <see cref="System.Console.Error"/>. Setting this argument to null, will disable help screen.</param>
        public CommandLineParserSettings(bool caseSensitive, TextWriter helpWriter)
        {
            CaseSensitive = caseSensitive;
            HelpWriter = helpWriter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class,
        /// setting case comparison and mutually exclusive behaviors.
        /// </summary>
        /// <param name="caseSensitive">If set to true, parsing will be case sensitive.</param>
        /// <param name="mutuallyExclusive">If set to true, enable mutually exclusive behavior.</param>
        public CommandLineParserSettings(bool caseSensitive, bool mutuallyExclusive)
        {
            CaseSensitive = caseSensitive;
            MutuallyExclusive = mutuallyExclusive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class,
        /// setting case comparison, mutually exclusive behavior and help output option.
        /// </summary>
        /// <param name="caseSensitive">If set to true, parsing will be case sensitive.</param>
        /// <param name="mutuallyExclusive">If set to true, enable mutually exclusive behavior.</param>
        /// <param name="helpWriter">Any instance derived from <see cref="System.IO.TextWriter"/>,
        /// default <see cref="System.Console.Error"/>. Setting this argument to null, will disable help screen.</param>
        public CommandLineParserSettings(bool caseSensitive, bool mutuallyExclusive, TextWriter helpWriter)
        {
            CaseSensitive = caseSensitive;
            MutuallyExclusive = mutuallyExclusive;
            HelpWriter = helpWriter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.CommandLineParserSettings"/> class,
        /// setting case comparison, mutually exclusive behavior and help output option.
        /// </summary>
        /// <param name="caseSensitive">If set to true, parsing will be case sensitive.</param>
        /// <param name="mutuallyExclusive">If set to true, enable mutually exclusive behavior.</param>
        /// <param name="ignoreUnknownArguments">If set to true, allow the parser to skip unknown argument, otherwise return a parse failure</param>
        /// <param name="helpWriter">Any instance derived from <see cref="System.IO.TextWriter"/>,
        /// default <see cref="System.Console.Error"/>. Setting this argument to null, will disable help screen.</param>
        public CommandLineParserSettings(bool caseSensitive, bool mutuallyExclusive, bool ignoreUnknownArguments, TextWriter helpWriter)
        {
            CaseSensitive = caseSensitive;
            MutuallyExclusive = mutuallyExclusive;
            HelpWriter = helpWriter;
            IgnoreUnknownArguments = ignoreUnknownArguments;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the case comparison behavior.
        /// Default is set to true.
        /// </summary>
        public bool CaseSensitive
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.IO.TextWriter"/> used for help method output.
        /// Setting this property to null, will disable help screen.
        /// </summary>
        public TextWriter HelpWriter
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the parser shall move on to the next argument and ignore the given argument if it
        /// encounter an unknown arguments
        /// </summary>
        /// <value>
        /// <c>true</c> to allow parsing the arguments with differents class options that do not have all the arguments.
        /// </value>
        /// <remarks>
        /// This allows fragmented version class parsing, useful for project with addon where addons also requires command line arguments but
        /// when these are unknown by the main program at build time.
        /// </remarks>
        public bool IgnoreUnknownArguments
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the mutually exclusive behavior.
        /// Default is set to false.
        /// </summary>
        public bool MutuallyExclusive
        {
            get; set;
        }

        #endregion Properties
    }

    /// <summary>
    /// Indicates the instance method that must be invoked when it becomes necessary show your help screen.
    /// The method signature is an instance method with no parameters and String
    /// return value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,
            AllowMultiple=false,
            Inherited=true)]
    public sealed class HelpOptionAttribute : BaseOptionAttribute
    {
        #region Fields

        private const string DEFAULT_HELP_TEXT = "Display this help screen.";

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.HelpOptionAttribute"/> class.
        /// </summary>
        public HelpOptionAttribute()
            : this(null, "help")
        {
            HelpText = DEFAULT_HELP_TEXT;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.HelpOptionAttribute"/> class.
        /// Allows you to define short and long option names.
        /// </summary>
        /// <param name="shortName">The short name of the option or null if not used.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public HelpOptionAttribute(string shortName, string longName)
        {
            ShortName = shortName;
            LongName = longName;
            HelpText = DEFAULT_HELP_TEXT;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Returns always false for this kind of option.
        /// This behaviour can't be changed by design; if you try set <see cref="CommandLine.HelpOptionAttribute.Required"/>
        /// an <see cref="System.InvalidOperationException"/> will be thrown.
        /// </summary>
        public override bool Required
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        #endregion Properties

        #region Methods

        public static void InvokeMethod(object target,
            Pair<MethodInfo, HelpOptionAttribute> pair, out string text)
        {
            text = null;

            var method = pair.Left;
            if (!CheckMethodSignature(method))
            {
                throw new MemberAccessException();
            }

            text = (string)method.Invoke(target, null);
        }

        private static bool CheckMethodSignature(MethodInfo value)
        {
            return value.ReturnType == typeof(string) && value.GetParameters().Length == 0;
        }

        #endregion Methods
    }

    public sealed class LongOptionParser : ArgumentParser
    {
        #region Fields

        private readonly bool _ignoreUnkwnownArguments;

        #endregion Fields

        #region Constructors

        public LongOptionParser(bool ignoreUnkwnownArguments)
        {
            _ignoreUnkwnownArguments = ignoreUnkwnownArguments;
        }

        #endregion Constructors

        #region Methods

        public override ParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options)
        {
            var parts = argumentEnumerator.Current.Substring(2).Split(new[] { '=' }, 2);
            var option = map[parts[0]];
            bool valueSetting;

            if (option == null)
            {
                return _ignoreUnkwnownArguments ? ParserState.MoveOnNextElement : ParserState.Failure;
            }

            option.IsDefined = true;

            ArgumentParser.EnsureOptionArrayAttributeIsNotBoundToScalar(option);

            if (!option.IsBoolean)
            {
                if (parts.Length == 1 && (argumentEnumerator.IsLast || !ArgumentParser.IsInputValue(argumentEnumerator.Next)))
                {
                    return ParserState.Failure;
                }

                if (parts.Length == 2)
                {
                    if (!option.IsArray)
                    {
                        valueSetting = option.SetValue(parts[1], options);
                        if (!valueSetting)
                        {
                            this.DefineOptionThatViolatesFormat(option);
                        }

                        return ArgumentParser.BooleanToParserState(valueSetting);
                    }

                    ArgumentParser.EnsureOptionAttributeIsArrayCompatible(option);

                    var items = ArgumentParser.GetNextInputValues(argumentEnumerator);
                    items.Insert(0, parts[1]);

                    valueSetting = option.SetValue(items, options);
                    if (!valueSetting)
                    {
                        this.DefineOptionThatViolatesFormat(option);
                    }

                    return ArgumentParser.BooleanToParserState(valueSetting);
                } else
                {
                    if (!option.IsArray)
                    {
                        valueSetting = option.SetValue(argumentEnumerator.Next, options);
                        if (!valueSetting)
                        {
                            this.DefineOptionThatViolatesFormat(option);
                        }

                        return ArgumentParser.BooleanToParserState(valueSetting, true);
                    }

                    ArgumentParser.EnsureOptionAttributeIsArrayCompatible(option);

                    var items = ArgumentParser.GetNextInputValues(argumentEnumerator);

                    valueSetting = option.SetValue(items, options);
                    if (!valueSetting)
                    {
                        this.DefineOptionThatViolatesFormat(option);
                    }

                    //return ArgumentParser.BooleanToParserState(valueSetting, true);
                    return ArgumentParser.BooleanToParserState(valueSetting);
                }
            }

            if (parts.Length == 2)
            {
                return ParserState.Failure;
            }

            valueSetting = option.SetValue(true, options);
            if (!valueSetting)
            {
                this.DefineOptionThatViolatesFormat(option);
            }

            return ArgumentParser.BooleanToParserState(valueSetting);
        }

        #endregion Methods
    }

    public sealed class OneCharStringEnumerator : IArgumentEnumerator
    {
        #region Fields

        private readonly string _data;

        private string _currentElement;
        private int _index;

        #endregion Fields

        #region Constructors

        public OneCharStringEnumerator(string value)
        {
            Assumes.NotNullOrEmpty(value, "value");

            _data = value;
            _index = -1;
        }

        #endregion Constructors

        #region Properties

        public string Current
        {
            get {
                if (_index == -1)
                {
                    throw new InvalidOperationException();
                }

                if (_index >= _data.Length)
                {
                    throw new InvalidOperationException();
                }

                return _currentElement;
            }
        }

        public bool IsLast
        {
            get { return _index == _data.Length - 1; }
        }

        public string Next
        {
            get {
                if (_index == -1)
                {
                    throw new InvalidOperationException();
                }

                if (_index > _data.Length)
                {
                    throw new InvalidOperationException();
                }

                if (IsLast)
                {
                    return null;
                }

                return _data.Substring(_index + 1, 1);
            }
        }

        #endregion Properties

        #region Methods

        public string GetRemainingFromNext()
        {
            if (_index == -1)
            {
                throw new InvalidOperationException();
            }

            if (_index > _data.Length)
            {
                throw new InvalidOperationException();
            }

            return _data.Substring(_index + 1);
        }

        void IDisposable.Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_index < (_data.Length - 1))
            {
                _index++;
                _currentElement = _data.Substring(_index, 1);
                return true;
            }
            _index = _data.Length;

            return false;
        }

        public bool MovePrevious()
        {
            throw new NotSupportedException();
        }

        public void Reset()
        {
            _index = -1;
        }

        #endregion Methods
    }

    /// <summary>
    /// Models an option that can accept multiple values as separated arguments.
    /// </summary>
    public sealed class OptionArrayAttribute : OptionAttribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionArrayAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option or null if not used.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionArrayAttribute(string shortName, string longName)
            : base(shortName, longName)
        {
        }

        #endregion Constructors
    }

    /// <summary>
    /// Models an option specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property , AllowMultiple=false, Inherited=true)]
    public class OptionAttribute : BaseOptionAttribute
    {
        #region Fields

        public const string DefaultMutuallyExclusiveSet = "Default";

        private readonly string _uniqueName;

        private string _mutuallyExclusiveSet;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option or null if not used.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionAttribute(string shortName, string longName)
        {
            if (!string.IsNullOrEmpty(shortName))
            {
                _uniqueName = shortName;
            } else if (!string.IsNullOrEmpty(longName))
            {
                _uniqueName = longName;
            }

            if (_uniqueName == null)
            {
                throw new InvalidOperationException();
            }

            base.ShortName = shortName;
            base.LongName = longName;
        }

        #endregion Constructors

        #if UNIT_TESTS

        public OptionInfo CreateOptionInfo()
        {
            return new OptionInfo(base.ShortName, base.LongName);
        }

        #endif

        #region Properties

        /// <summary>
        /// Gets or sets the option's mutually exclusive set.
        /// </summary>
        public string MutuallyExclusiveSet
        {
            get { return _mutuallyExclusiveSet; }
            set {
                if (string.IsNullOrEmpty(value))
                {
                    _mutuallyExclusiveSet = OptionAttribute.DefaultMutuallyExclusiveSet;
                } else
                {
                    _mutuallyExclusiveSet = value;
                }
            }
        }

        public string UniqueName
        {
            get { return _uniqueName; }
        }

        #endregion Properties
    }

    public sealed class OptionGroupParser : ArgumentParser
    {
        #region Fields

        private readonly bool _ignoreUnkwnownArguments;

        #endregion Fields

        #region Constructors

        public OptionGroupParser(bool ignoreUnkwnownArguments)
        {
            _ignoreUnkwnownArguments = ignoreUnkwnownArguments;
        }

        #endregion Constructors

        #region Methods

        public override ParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options)
        {
            IArgumentEnumerator group = new OneCharStringEnumerator(argumentEnumerator.Current.Substring(1));
            while (group.MoveNext())
            {
                var option = map[group.Current];
                if (option == null)
                {
                    return _ignoreUnkwnownArguments ? ParserState.MoveOnNextElement : ParserState.Failure;
                }

                option.IsDefined = true;

                ArgumentParser.EnsureOptionArrayAttributeIsNotBoundToScalar(option);

                if (!option.IsBoolean)
                {
                    if (argumentEnumerator.IsLast && group.IsLast)
                    {
                        return ParserState.Failure;
                    }

                    bool valueSetting;
                    if (!group.IsLast)
                    {
                        if (!option.IsArray)
                        {
                            valueSetting = option.SetValue(group.GetRemainingFromNext(), options);
                            if (!valueSetting)
                            {
                                this.DefineOptionThatViolatesFormat(option);
                            }

                            return ArgumentParser.BooleanToParserState(valueSetting);
                        }

                        ArgumentParser.EnsureOptionAttributeIsArrayCompatible(option);

                        var items = ArgumentParser.GetNextInputValues(argumentEnumerator);
                        items.Insert(0,  @group.GetRemainingFromNext());

                        valueSetting = option.SetValue(items, options);
                        if (!valueSetting)
                        {
                            this.DefineOptionThatViolatesFormat(option);
                        }

                        return ArgumentParser.BooleanToParserState(valueSetting, true);
                    }

                    if (!argumentEnumerator.IsLast && !ArgumentParser.IsInputValue(argumentEnumerator.Next))
                    {
                        return ParserState.Failure;
                    } else
                    {
                        if (!option.IsArray)
                        {
                            valueSetting = option.SetValue(argumentEnumerator.Next, options);
                            if (!valueSetting)
                            {
                                this.DefineOptionThatViolatesFormat(option);
                            }

                            return ArgumentParser.BooleanToParserState(valueSetting, true);
                        }

                        ArgumentParser.EnsureOptionAttributeIsArrayCompatible(option);

                        var items = ArgumentParser.GetNextInputValues(argumentEnumerator);

                        valueSetting = option.SetValue(items, options);
                        if (!valueSetting)
                        {
                            this.DefineOptionThatViolatesFormat(option);
                        }

                        return ArgumentParser.BooleanToParserState(valueSetting);
                    }
                }

                if (!@group.IsLast && map[@group.Next] == null)
                {
                    return ParserState.Failure;
                }

                if (!option.SetValue(true, options))
                {
                    return ParserState.Failure;
                }
            }

            return ParserState.Success;
        }

        #endregion Methods
    }

    [DebuggerDisplay("ShortName = {ShortName}, LongName = {LongName}")]
    public sealed class OptionInfo
    {
        #region Fields

        private readonly OptionAttribute _attribute;
        private readonly object _defaultValue;
        private readonly bool _hasDefaultValue;
        private readonly string _helpText;
        private readonly string _longName;
        private readonly string _mutuallyExclusiveSet;
        private readonly PropertyInfo _property;
        private readonly bool _required;
        private readonly object _setValueLock = new object();
        private readonly string _shortName;

        #endregion Fields

        #region Constructors

        public OptionInfo(OptionAttribute attribute, PropertyInfo property)
        {
            if (attribute != null)
            {
                _required = attribute.Required;
                _helpText = attribute.HelpText;
                _shortName = attribute.ShortName;
                _longName = attribute.LongName;
                _mutuallyExclusiveSet = attribute.MutuallyExclusiveSet;
                _defaultValue = attribute.DefaultValue;
                _hasDefaultValue = attribute.HasDefaultValue;
                _attribute = attribute;
            } else
            {
                throw new ArgumentNullException("attribute", "The attribute is mandatory");
            }

            if (property != null)
            {
                _property = property;
            } else
            {
                throw new ArgumentNullException("property", "The property is mandatory");
            }
        }

        #endregion Constructors

        #if UNIT_TESTS

        public OptionInfo(string shortName, string longName)
        {
            _shortName = shortName;
            _longName = longName;
        }

        #endif

        #region Properties

        public bool HasBothNames
        {
            get { return (_shortName != null && _longName != null); }
        }

        public string HelpText
        {
            get { return _helpText; }
        }

        public bool IsArray
        {
            get { return _property.PropertyType.IsArray; }
        }

        public bool IsAttributeArrayCompatible
        {
            get { return _attribute is OptionArrayAttribute; }
        }

        public bool IsBoolean
        {
            get { return _property.PropertyType == typeof(bool); }
        }

        public bool IsDefined
        {
            get; set;
        }

        public string LongName
        {
            get { return _longName; }
        }

        public string MutuallyExclusiveSet
        {
            get { return _mutuallyExclusiveSet; }
        }

        public string NameWithSwitch
        {
            get {
                if (_longName != null)
                {
                    return string.Concat("--", _longName);
                }

                return string.Concat("-", _shortName);
            }
        }

        public bool Required
        {
            get { return _required; }
        }

        public string ShortName
        {
            get { return _shortName; }
        }

        #endregion Properties

        #region Methods

        public static OptionMap CreateMap(object target, CommandLineParserSettings settings)
        {
            var list = ReflectionUtil.RetrievePropertyList<OptionAttribute>(target);
            if (list != null)
            {
                var map = new OptionMap(list.Count, settings);

                foreach (var pair in list)
                {
                    if (pair != null && pair.Right != null)
                    {
                        map[pair.Right.UniqueName] = new OptionInfo(pair.Right, pair.Left);
                    }
                }

                map.RawOptions = target;

                return map;
            }

            return null;
        }

        public void SetDefault(object options)
        {
            if (_hasDefaultValue)
            {
                lock(_setValueLock)
                {
                    try
                    {
                        _property.SetValue(options, _defaultValue, null);
                    } catch (Exception e)
                    {
                        throw new CommandLineParserException("Bad default value.", e);
                    }
                }
            }
        }

        public bool SetValue(string value, object options)
        {
            if (_attribute is OptionListAttribute)
            {
                return SetValueList(value, options);
            }

            if (ReflectionUtil.IsNullableType(_property.PropertyType))
            {
                return SetNullableValue(value, options);
            }

            return SetValueScalar(value, options);
        }

        public bool SetValue(IList<string> values, object options)
        {
            Type elementType = _property.PropertyType.GetElementType();
            Array array = Array.CreateInstance(elementType, values.Count);

            for (int i = 0; i < array.Length; i++)
            {
                try
                {
                    lock(_setValueLock)
                    {
                        //array.SetValue(Convert.ChangeType(values[i], elementType, CultureInfo.InvariantCulture), i);
                        array.SetValue(Convert.ChangeType(values[i], elementType, Thread.CurrentThread.CurrentCulture), i);
                        _property.SetValue(options, array, null);
                    }
                } catch (FormatException)
                {
                    return false;
                }
            }

            return true;
        }

        public bool SetValue(bool value, object options)
        {
            lock(_setValueLock)
            {
                _property.SetValue(options, value, null);

                return true;
            }
        }

        private bool SetNullableValue(string value, object options)
        {
            var nc = new NullableConverter(_property.PropertyType);

            try
            {
                lock(_setValueLock)
                {
                    //_property.SetValue(options, nc.ConvertFromString(null, CultureInfo.InvariantCulture, value), null);
                    _property.SetValue(options, nc.ConvertFromString(null, Thread.CurrentThread.CurrentCulture, value), null);
                }
            }
            // the FormatException (thrown by ConvertFromString) is thrown as Exception.InnerException,
            // so we've catch directly Exception
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool SetValueList(string value, object options)
        {
            lock(_setValueLock)
            {
                _property.SetValue(options, new List<string>(), null);

                var fieldRef = (IList<string>)_property.GetValue(options, null);
                var values = value.Split(((OptionListAttribute)_attribute).Separator);

                for (int i = 0; i < values.Length; i++)
                {
                    fieldRef.Add(values[i]);
                }

                return true;
            }
        }

        private bool SetValueScalar(string value, object options)
        {
            try
            {
                if (_property.PropertyType.IsEnum)
                {
                    lock(_setValueLock)
                    {
                        _property.SetValue(options, Enum.Parse(_property.PropertyType, value, true), null);
                    }
                } else
                {
                    lock(_setValueLock)
                    {
                        //_property.SetValue(options, Convert.ChangeType(value, _property.PropertyType, CultureInfo.InvariantCulture), null);
                        _property.SetValue(options, Convert.ChangeType(value, _property.PropertyType, Thread.CurrentThread.CurrentCulture), null);
                    }
                }
            } catch (InvalidCastException) // Convert.ChangeType
            {
                return false;
            } catch (FormatException) // Convert.ChangeType
            {
                return false;
            } catch (ArgumentException) // Enum.Parse
            {
                return false;
            }

            return true;
        }

        #endregion Methods
    }

    /// <summary>
    /// Models an option that can accept multiple values.
    /// Must be applied to a field compatible with an 
    /// IList&lt;T&gt; interface of String instances.
    /// </summary>
    public sealed class OptionListAttribute : OptionAttribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="CommandLine.OptionListAttribute"/> class.
        /// </summary>
        /// 
        /// <param name="shortName">
        /// The short name of the option or null if not used.
        /// </param>
        /// 
        /// <param name="longName">
        /// The long name of the option or null if not used.
        /// </param>
        public OptionListAttribute(string shortName, string longName)
            : base(shortName, longName)
        {
            Separator = ':';
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="CommandLine.OptionListAttribute"/> class.
        /// </summary>
        /// 
        /// <param name="shortName">
        /// The short name of the option or null if not used.
        /// </param>
        /// 
        /// <param name="longName">
        /// The long name of the option or null if not used.
        /// </param>
        /// 
        /// <param name="separator">
        /// Values separator character.
        /// </param>
        public OptionListAttribute(string shortName, string longName, char separator)
            : base(shortName, longName)
        {
            Separator = separator;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the values separator character.
        /// </summary>
        public char Separator
        {
            get; set;
        }

        #endregion Properties
    }

    public sealed class OptionMap
    {
        #region Fields

        readonly Dictionary<string, OptionInfo> _map;
        readonly Dictionary<string, MutuallyExclusiveInfo> _mutuallyExclusiveSetMap;
        readonly Dictionary<string, string> _names;
        readonly CommandLineParserSettings _settings;

        #endregion Fields

        #region Constructors

        public OptionMap(int capacity, CommandLineParserSettings settings)
        {
            _settings = settings;

            IEqualityComparer<string> comparer;
            if (_settings.CaseSensitive)
            {
                comparer = StringComparer.Ordinal;
            } else
            {
                comparer = StringComparer.OrdinalIgnoreCase;
            }

            _names = new Dictionary<string, string>(capacity, comparer);
            _map = new Dictionary<string, OptionInfo>(capacity * 2, comparer);

            if (_settings.MutuallyExclusive)
            {
                //_mutuallyExclusiveSetMap = new Dictionary<string, int>(capacity, StringComparer.OrdinalIgnoreCase);
                _mutuallyExclusiveSetMap = new Dictionary<string, MutuallyExclusiveInfo>(capacity, StringComparer.OrdinalIgnoreCase);
            }
        }

        #endregion Constructors

        #region Properties

        public object RawOptions
        {
            private get; set;
        }

        #endregion Properties

        #region Indexers

        public OptionInfo this[string key]
        {
            get {
                OptionInfo option = null;

                if (_map.ContainsKey(key))
                {
                    option = _map[key];
                } else
                {
                    if (_names.ContainsKey(key))
                    {
                        var optionKey = _names[key];
                        option = _map[optionKey];
                    }
                }

                return option;
            }
            set {
                _map[key] = value;

                if (value.HasBothNames)
                {
                    _names[value.LongName] = value.ShortName;
                }
            }
        }

        #endregion Indexers

        #region Methods

        public bool EnforceRules()
        {
            return EnforceMutuallyExclusiveMap() && EnforceRequiredRule();
        }

        public void SetDefaults()
        {
            foreach (OptionInfo option in _map.Values)
            {
                option.SetDefault(this.RawOptions);
            }
        }

        private static void BuildAndSetPostParsingStateIfNeeded(object options, OptionInfo option, bool? required, bool? mutualExclusiveness)
        {
            var commandLineOptionsBase = options as CommandLineOptionsBase;
            if (commandLineOptionsBase == null)
            {
                return;
            }

            var error = new ParsingError {
                BadOption = {
                    ShortName = option.ShortName,
                    LongName = option.LongName
                }
            };

            if (required != null)
            {
                error.ViolatesRequired = required.Value;
            }
            if (mutualExclusiveness != null)
            {
                error.ViolatesMutualExclusiveness = mutualExclusiveness.Value;
            }

            (commandLineOptionsBase).InternalLastPostParsingState.Errors.Add(error);
        }

        private void BuildMutuallyExclusiveMap(OptionInfo option)
        {
            var setName = option.MutuallyExclusiveSet;

            if (!_mutuallyExclusiveSetMap.ContainsKey(setName))
            {
                //_mutuallyExclusiveSetMap.Add(setName, 0);
                _mutuallyExclusiveSetMap.Add(setName, new MutuallyExclusiveInfo(option));
            }

            _mutuallyExclusiveSetMap[setName].IncrementOccurrence();
        }

        private bool EnforceMutuallyExclusiveMap()
        {
            if (!_settings.MutuallyExclusive)
            {
                return true;
            }

            foreach (OptionInfo option in _map.Values)
            {
                if (option.IsDefined && option.MutuallyExclusiveSet != null)
                {
                    BuildMutuallyExclusiveMap(option);
                }
            }

            //foreach (int occurrence in _mutuallyExclusiveSetMap.Values)
            foreach (MutuallyExclusiveInfo info in _mutuallyExclusiveSetMap.Values)
            {
                if (info.Occurrence > 1) //if (occurrence > 1)
                {
                    //BuildAndSetPostParsingStateIfNeeded(this.RawOptions, null, null, true);
                    BuildAndSetPostParsingStateIfNeeded(this.RawOptions, info.BadOption, null, true);
                    return false;
                }
            }

            return true;
        }

        private bool EnforceRequiredRule()
        {
            foreach (OptionInfo option in _map.Values)
            {
                if (option.Required && !option.IsDefined)
                {
                    BuildAndSetPostParsingStateIfNeeded(this.RawOptions, option, true, null);
                    return false;
                }
            }
            return true;
        }

        #endregion Methods

        #region Nested Types

        sealed class MutuallyExclusiveInfo
        {
            #region Fields

            int _count;

            #endregion Fields

            #region Constructors

            public MutuallyExclusiveInfo(OptionInfo option)
            {
                BadOption = option;
            }

            #endregion Constructors

            #region Properties

            public OptionInfo BadOption
            {
                get; private set;
            }

            public int Occurrence
            {
                get { return _count; }
            }

            #endregion Properties

            #region Methods

            public void IncrementOccurrence()
            {
                ++_count;
            }

            #endregion Methods
        }

        #endregion Nested Types
    }

    public sealed class Pair<TLeft, TRight>
        where TLeft : class
        where TRight : class
    {
        #region Fields

        private readonly TLeft _left;
        private readonly TRight _right;

        #endregion Fields

        #region Constructors

        public Pair(TLeft left, TRight right)
        {
            _left = left;
            _right = right;
        }

        #endregion Constructors

        #region Properties

        public TLeft Left
        {
            get { return _left; }
        }

        public TRight Right
        {
            get { return _right; }
        }

        #endregion Properties

        #region Methods

        public override bool Equals(object obj)
        {
            var other = obj as Pair<TLeft, TRight>;

            if (other == null)
            {
                return false;
            }

            return Equals(_left, other._left) && Equals(_right, other._right);
        }

        public override int GetHashCode()
        {
            int leftHash = (_left == null ? 0 : _left.GetHashCode());
            int rightHash = (_right == null ? 0 : _right.GetHashCode());

            return leftHash ^ rightHash;
        }

        #endregion Methods
    }

    /// <summary>
    /// Models a parsing error.
    /// </summary>
    public class ParsingError
    {
        #region Constructors

        public ParsingError()
        {
            this.BadOption = new BadOptionInfo();
        }

        public ParsingError(string shortName, string longName, bool format)
        {
            //this.BadOptionShortName = shortName;
            //this.BadOptionLongName = longName;
            this.BadOption = new BadOptionInfo(shortName, longName);

            this.ViolatesFormat = format;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or a the bad parsed option.
        /// </summary>
        /// <value>
        /// The bad option.
        /// </value>
        public BadOptionInfo BadOption
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CommandLine.ParsingError"/> violates format.
        /// </summary>
        /// <value>
        /// <c>true</c> if violates format; otherwise, <c>false</c>.
        /// </value>
        public bool ViolatesFormat
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CommandLine.ParsingError"/> violates mutual exclusiveness.
        /// </summary>
        /// <value>
        /// <c>true</c> if violates mutual exclusiveness; otherwise, <c>false</c>.
        /// </value>
        public bool ViolatesMutualExclusiveness
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CommandLine.ParsingError"/> violates required.
        /// </summary>
        /// <value>
        /// <c>true</c> if violates required; otherwise, <c>false</c>.
        /// </value>
        public bool ViolatesRequired
        {
            get; set;
        }

        #endregion Properties
    }

    /// <summary>
    /// Models a type that records the parser state afeter parsing.
    /// </summary>
    public sealed class PostParsingState
    {
        #region Constructors

        public PostParsingState()
        {
            Errors = new List<ParsingError>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets a list of parsing errors.
        /// </summary>
        /// <value>
        /// Parsing errors.
        /// </value>
        public List<ParsingError> Errors
        {
            get; private set;
        }

        #endregion Properties
    }

    public static class ReflectionUtil
    {
        #region Methods

        public static TAttribute GetAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            object[] a = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(TAttribute), false);
            if (a == null || a.Length <= 0)
            {
                return null;
            }
            return (TAttribute)a[0];
        }

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Pair<MethodInfo, TAttribute> RetrieveMethod<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var info = target.GetType().GetMethods();

            foreach (MethodInfo method in info)
            {
                if (!method.IsStatic)
                {
                    Attribute attribute =
                        Attribute.GetCustomAttribute(method, typeof(TAttribute), false);
                    if (attribute != null)
                    {
                        return new Pair<MethodInfo, TAttribute>(method, (TAttribute)attribute);
                    }
                }
            }

            return null;
        }

        public static TAttribute RetrieveMethodAttributeOnly<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var info = target.GetType().GetMethods();

            foreach (MethodInfo method in info)
            {
                if (!method.IsStatic)
                {
                    Attribute attribute =
                        Attribute.GetCustomAttribute(method, typeof(TAttribute), false);
                    if (attribute != null)
                    {
                        return (TAttribute)attribute;
                    }
                }
            }

            return null;
        }

        public static IList<TAttribute> RetrievePropertyAttributeList<TAttribute>(object target)
            where TAttribute : Attribute
        {
            IList<TAttribute> list = new List<TAttribute>();
            var info = target.GetType().GetProperties();

            foreach (var property in info)
            {
                if (property != null && (property.CanRead && property.CanWrite))
                {
                    var setMethod = property.GetSetMethod();
                    if (setMethod != null && !setMethod.IsStatic)
                    {
                        var attribute = Attribute.GetCustomAttribute(property, typeof(TAttribute), false);
                        if (attribute != null)
                        {
                            list.Add((TAttribute)attribute);
                        }
                    }
                }
            }

            return list;
        }

        public static IList<Pair<PropertyInfo, TAttribute>> RetrievePropertyList<TAttribute>(object target)
            where TAttribute : Attribute
        {
            IList<Pair<PropertyInfo, TAttribute>> list = new List<Pair<PropertyInfo, TAttribute>>();
            if (target != null)
            {
                var propertiesInfo = target.GetType().GetProperties();

                foreach (var property in propertiesInfo)
                {
                    if (property != null && (property.CanRead && property.CanWrite))
                    {
                        var setMethod = property.GetSetMethod();
                        if (setMethod != null && !setMethod.IsStatic)
                        {
                            var attribute = Attribute.GetCustomAttribute(property, typeof(TAttribute), false);
                            if (attribute != null)
                            {
                                list.Add(new Pair<PropertyInfo, TAttribute>(property, (TAttribute)attribute));
                            }
                        }
                    }
                }
            }

            return list;
        }

        #endregion Methods
    }

    public sealed class StringArrayEnumerator : IArgumentEnumerator
    {
        #region Fields

        private readonly string[] _data;
        private readonly int _endIndex;

        private int _index;

        #endregion Fields

        #region Constructors

        public StringArrayEnumerator(string[] value)
        {
            Assumes.NotNull(value, "value");

            _data = value;
            _index = -1;
            _endIndex = value.Length;
        }

        #endregion Constructors

        #region Properties

        public string Current
        {
            get {
                if (_index == -1)
                {
                    throw new InvalidOperationException();
                }
                if (_index >= _endIndex)
                {
                    throw new InvalidOperationException();
                }
                return _data[_index];
            }
        }

        public bool IsLast
        {
            get { return _index == _endIndex - 1; }
        }

        public string Next
        {
            get {
                if (_index == -1)
                {
                    throw new InvalidOperationException();
                }
                if (_index > _endIndex)
                {
                    throw new InvalidOperationException();
                }
                if (IsLast)
                {
                    return null;
                }
                return _data[_index + 1];
            }
        }

        #endregion Properties

        #region Methods

        public string GetRemainingFromNext()
        {
            throw new NotSupportedException();
        }

        void IDisposable.Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_index < _endIndex)
            {
                _index++;
                return _index < _endIndex;
            }
            return false;
        }

        public bool MovePrevious()
        {
            if (_index <= 0)
            {
                throw new InvalidOperationException();
            }
            if (_index <= _endIndex)
            {
                _index--;
                return _index <= _endIndex;
            }
            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        #endregion Methods
    }

    public static class StringUtil
    {
        #region Methods

        public static string Spaces(int count)
        {
            return new String(' ', count);
        }

        #endregion Methods
    }

    public class TargetWrapper
    {
        #region Fields

        private readonly object _target;
        private readonly IList<string> _valueList;
        private readonly ValueListAttribute _vla;

        #endregion Fields

        #region Constructors

        public TargetWrapper(object target)
        {
            _target = target;
            _vla = ValueListAttribute.GetAttribute(_target);
            if (IsValueListDefined)
            {
                _valueList = ValueListAttribute.GetReference(_target);
            }
        }

        #endregion Constructors

        #region Properties

        public bool IsValueListDefined
        {
            get { return _vla != null; }
        }

        #endregion Properties

        #region Methods

        public bool AddValueItemIfAllowed(string item)
        {
            if (_vla.MaximumElements == 0 || _valueList.Count == _vla.MaximumElements)
            {
                return false;
            }

            lock(this)
            {
                _valueList.Add(item);
            }

            return true;
        }

        #endregion Methods
    }

    /// <summary>
    /// Models a list of command line arguments that are not options.
    /// Must be applied to a field compatible with an IList&lt;T&gt; interface
    /// of String instances.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,
            AllowMultiple=false,
            Inherited=true)]
    public sealed class ValueListAttribute : Attribute
    {
        #region Fields

        private readonly Type _concreteType;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.ValueListAttribute"/> class.
        /// </summary>
        /// <param name="concreteType">A type that implements IList&lt;T&gt;.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="concreteType"/> is null.</exception>
        public ValueListAttribute(Type concreteType)
            : this()
        {
            if (concreteType == null)
            {
                throw new ArgumentNullException("concreteType");
            }

            if (!typeof(IList<string>).IsAssignableFrom(concreteType))
            {
                throw new CommandLineParserException("The types are incompatible.");
            }

            _concreteType = concreteType;
        }

        private ValueListAttribute()
        {
            MaximumElements = -1;
        }

        #endregion Constructors

        #region Properties

        public Type ConcreteType
        {
            get { return _concreteType; }
        }

        /// <summary>
        /// Gets or sets the maximum element allow for the list managed by <see cref="CommandLine.ValueListAttribute"/> type.
        /// If lesser than 0, no upper bound is fixed.
        /// If equal to 0, no elements are allowed.
        /// </summary>
        public int MaximumElements
        {
            get; set;
        }

        #endregion Properties

        #region Methods

        public static ValueListAttribute GetAttribute(object target)
        {
            var list = ReflectionUtil.RetrievePropertyList<ValueListAttribute>(target);
            if (list == null || list.Count == 0)
            {
                return null;
            }

            if (list.Count > 1)
            {
                throw new InvalidOperationException();
            }

            var pairZero = list[0];

            return pairZero.Right;
        }

        public static IList<string> GetReference(object target)
        {
            Type concreteType;
            var property = GetProperty(target, out concreteType);

            if (property == null || concreteType == null)
            {
                return null;
            }

            property.SetValue(target, Activator.CreateInstance(concreteType), null);

            return (IList<string>)property.GetValue(target, null);
        }

        private static PropertyInfo GetProperty(object target, out Type concreteType)
        {
            concreteType = null;

            var list = ReflectionUtil.RetrievePropertyList<ValueListAttribute>(target);
            if (list == null || list.Count == 0)
            {
                return null;
            }

            if (list.Count > 1)
            {
                throw new InvalidOperationException();
            }

            var pairZero = list[0];
            concreteType = pairZero.Right.ConcreteType;

            return pairZero.Left;
        }

        #endregion Methods
    }
}