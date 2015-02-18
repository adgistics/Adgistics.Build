#region Header

// ****************************************************************************
// * Copyright (c) Adgistics Limited and others. All rights reserved.
// * The contents of this file are subject to the terms of the
// * Adgistics Development and Distribution License (the "License").
// * You may not use this file except in compliance with the License.
// *
// * http://www.adgistics.com/license.html
// *
// * See the License for the specific language governing permissions
// * and limitations under the License.
// ****************************************************************************
//

#endregion Header

namespace NUnitReport
{
    using System;
    using System.Text;

    using CommandLine;

    public class Options : CommandLineOptionsBase
    {
        #region Properties

        /// <summary>
        ///   Gets or sets the input NUNit XML report. The file must exist.
        /// </summary>
        [Option("i", "intput", Required = true, 
                HelpText = "The root directory to copy the templates from.")]
        public string InputFile
        {
            get; set;
        }

        /// <summary>
        ///   Gets or sets the output directory.
        /// </summary>
        [Option("o", "output", Required = true, 
                HelpText = "The directory to output to, if it does not exist it will be created (if possible).")]
        public string OutputDirectory
        {
            get; set;
        }

        /// <summary>
        ///   Gets or sets the xslt directory.
        /// </summary>
        [Option("x", "xslt", Required = true, 
                HelpText = "The directory containing the XSLT files.")]
        public string XsltDirectory
        {
            get; set;
        }

        #endregion Properties

        #region Methods

        private String GetCopyright()
        {
            var builder = new StringBuilder(256);

            builder.AppendLine("   Copyright (c) Adgistics Limited and others. All rights reserved. ");
            builder.AppendLine("   The contents of this file are subject to the terms of the ");
            builder.AppendLine("   Adgistics Development and Distribution License (the 'License'). ");
            builder.AppendLine("   You may not use this file except in compliance with the License.");
            builder.AppendLine(" ");
            builder.AppendLine("      http://www.adgistics.com/license.html");
            builder.AppendLine(" ");
            builder.AppendLine("   See the License for the specific language governing permissions");
            builder.AppendLine("   and limitations under the License.");

            return builder.ToString();
        }

        private string GetExamples()
        {
            var builder = new StringBuilder(256);

            builder.AppendLine(" ");
            builder.AppendLine("Usage:");
            builder.AppendLine("       NUnitReport -i <template-directory> -o <output-directory>");
            builder.AppendLine(" ");

            return builder.ToString();
        }

        #endregion Methods
    }
}