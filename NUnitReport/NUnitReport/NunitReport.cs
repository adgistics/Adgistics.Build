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
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Xsl;

    using CommandLine;

    // TODO:2014-02-24:SM:Xslt directory resolution needs to be correctly automated.
    class NunitReport
    {
        #region Fields

        private static readonly string I18N = "i18n.xsl";
        private static readonly string XSLT = "NUnit-Frame.xsl";

        private readonly string Home;

        private string assembly;
        private string directory;
        private string parent;
        private string path;
        private XmlDocument xmDocument;

        #endregion Fields

        #region Constructors

        private NunitReport(Options options)
        {
            Options = options;
            Home = options.XsltDirectory;
            Directory.CreateDirectory(options.OutputDirectory);
        }

        #endregion Constructors

        #region Properties

        private Options Options
        {
            get; set;
        }

        #endregion Properties

        #region Methods

        public static void Main(string[] args)
        {
            var options = new Options();
            var parser = new CommandLineParser(
                new CommandLineParserSettings(Console.Error));
            parser.ParseArguments(args, options);

            // configure paths

            var main = new NunitReport(options);
            main.Run();
        }

        public void Run()
        {
            xmDocument = CreateSummaryXmlDoc();

            var source = new XmlDocument();
            source.Load(Options.InputFile);

            var node = xmDocument.ImportNode(source.DocumentElement, true);
            xmDocument.DocumentElement.AppendChild(node);

            var xpathNavigator = xmDocument.CreateNavigator();

            // Get All the test suites containing test-cases.
            var expr =
                xpathNavigator.Compile("//test-suite[(child::results/test-case)]");

            var iterator = xpathNavigator.Select(expr);

            while (iterator.MoveNext())
            {
                XPathNavigator xpathNavigator2 = iterator.Current;
                string testSuiteName = iterator.Current.GetAttribute("name", String.Empty);

                Console.WriteLine("Test Case: " + testSuiteName);

                // Get get the path for the current test-suite.
                XPathNodeIterator iterator2 =
                    xpathNavigator2.SelectAncestors(String.Empty, String.Empty, true);

                SetPathInformation(iterator2);

                Console.WriteLine("   Assembly:  " + assembly);
                Console.WriteLine("   Name:      " + directory);
                Console.WriteLine("   Parent:    " + parent);
                Console.WriteLine("   Path:      " + path);
                Console.WriteLine("   Directory: " + Options.OutputDirectory + path);
                Console.WriteLine("Generating output for: " + testSuiteName);

                CreateIndexHtml();

                CreateStyleSheetCss();

                CraeteOverviewSummaryHtml();

                CreateClassesFrameHtml();

                CreateOverviewFrameHtml();

                CreateTestSuiteHtml(testSuiteName);

                Console.WriteLine(" ");
            }
        }

        private void CraeteOverviewSummaryHtml()
        {
            Console.WriteLine("   outputing: overview-summary.html");
            // create the overview-summary.html at the root
            var stream = new StringReader(
                "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                "<xsl:include href=\"" + Path.Combine(Home, XSLT) + "\"/>" +
                "<xsl:template match=\"test-results\">" +
                "    <xsl:call-template name=\"overview\">" +
                "   <xsl:with-param name=\"path.dir\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                "   </xsl:call-template>" +
                " </xsl:template>" +
                " </xsl:stylesheet>");
            Write(stream, Path.Combine(Options.OutputDirectory, "overview-summary.html"));
        }

        private void CreateClassesFrameHtml()
        {
            Console.WriteLine("   outputing: classes-frame.html");
            // create the classes-frame.html at the root
            var stream = new StringReader(
                "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                "<xsl:include href=\"" + Path.Combine(Home, XSLT) + "\"/>" +
                "<xsl:template match=\"test-results\">" +
                "   <xsl:call-template name=\"classes\">" +
                "   <xsl:with-param name=\"path.dir\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                "   </xsl:call-template>" +
                " </xsl:template>" +
                " </xsl:stylesheet>");
            Write(stream, Path.Combine(Options.OutputDirectory, "classes-frame.html"));
        }

        private void CreateIndexHtml()
        {
            Console.WriteLine("   outputing: index.html");
            // create the index.html
            var stream = new StringReader(
                "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                "<xsl:include href=\"" + Path.Combine(Home, XSLT) + "\"/>" +
                "<xsl:template match=\"test-results\">" +
                "   <xsl:call-template name=\"index.html\">" +
                "   <xsl:with-param name=\"path.dir\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                "   </xsl:call-template>" +
                " </xsl:template>" +
                " </xsl:stylesheet>");
            Write(stream, Path.Combine(Options.OutputDirectory, "index.html"));
        }

        private void CreateOverviewFrameHtml()
        {
            Console.WriteLine("   outputing: overview-frame.html");
            // create the overview-frame.html at the root
            var stream = new StringReader(
                "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                "<xsl:include href=\"" + Path.Combine(Home, XSLT) + "\"/>" +
                "<xsl:template match=\"test-results\">" +
                "   <xsl:call-template name=\"assemblies\">" +
                "   <xsl:with-param name=\"path.dir\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                "   <xsl:with-param name=\"assembly.name\">" + assembly + "</xsl:with-param>" +
                "   </xsl:call-template>" +
                " </xsl:template>" +
                " </xsl:stylesheet>");
            Write(stream, Path.Combine(Options.OutputDirectory, "assemblies-frame.html"));
        }

        private void CreateStyleSheetCss()
        {
            Console.WriteLine("   outputing: stylesheet.css");
            // create the stylesheet.css
            var stream = new StringReader(
                "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                "<xsl:include href=\"" + Path.Combine(Home, XSLT) + "\"/>" +
                "<xsl:template match=\"test-results\">" +
                "   <xsl:call-template name=\"stylesheet.css\">" +
                "   <xsl:with-param name=\"path.dir\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                "   </xsl:call-template>" +
                " </xsl:template>" +
                " </xsl:stylesheet>");
            Write(stream, Path.Combine(Options.OutputDirectory, "stylesheet.css"));
        }

        private XmlDocument CreateSummaryXmlDoc()
        {
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("testsummary");
            root.SetAttribute("created", DateTime.Now.ToString());
            doc.AppendChild(root);

            return doc;
        }

        private void CreateTestSuiteHtml(string testSuiteName)
        {
            Console.WriteLine("   outputing: " + testSuiteName + ".html");
            // Build the "TestSuiteName".html file
            var stream = new StringReader(
                "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                "<xsl:include href=\"" + Path.Combine(Home, XSLT) + "\"/>" +
                "<xsl:template match=\"/\">" +
                "   <xsl:for-each select=\"//test-suite[@name='" + testSuiteName + "' and ancestor::test-suite[@name='" + parent + "'][position()=last()]]\">" +
                "       <xsl:call-template name=\"test.case\">" +
                "       <xsl:with-param name=\"path.dir\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                "       </xsl:call-template>" +
                "   </xsl:for-each>" +
                " </xsl:template>" +
                " </xsl:stylesheet>");
            Write(stream, Path.Combine(Path.Combine(Options.OutputDirectory, path), testSuiteName + ".html"));
        }

        private void SetPathInformation(XPathNodeIterator iterator)
        {
            path = String.Empty;
            parent = String.Empty;
            directory = String.Empty;
            assembly = String.Empty;

            int parentIndex = -1;

            while (iterator.MoveNext())
            {
                directory = iterator.Current.GetAttribute("name", String.Empty);

                if (directory != String.Empty
                    && directory.IndexOf(".dll", StringComparison.Ordinal) < 0)
                {
                    path = directory + "/" + path;
                }

                if (parentIndex == 1)
                {
                    parent = directory;
                }

                parentIndex++;
            }

            // Path is backwards at the moment
            string[] reverse = path.Split('/');
            path = String.Empty;
            if (IsUnix)
            {
                Array.Reverse(reverse);    
            }
            
            foreach (var element in reverse)
            {
                if (!String.IsNullOrWhiteSpace(element))
                {
                    path += (element + "/");
                }
            }

            string[] elements = parent.Split('\\');
            assembly = elements[elements.Length - 1];

            // Make sure our output directory exists
            Directory.CreateDirectory(Path.Combine(Options.OutputDirectory, path));
        }

        public static bool IsUnix
        {
            get
            {
                int p = (int) Environment.OSVersion.Platform;
                return (p == 4)  //Unix
                    || (p == 6); //OSx
            }
        }

        private void Write(TextReader stream, string fileName)
        {
            XmlTextReader reader;

            // Load the XmlTextReader from the stream
            reader = new XmlTextReader(stream);
            var xslTransform = new XslTransform();
            // Load the stylesheet from the stream.
            xslTransform.Load(reader);

            XPathDocument xmlDoc;
            var args = new XsltArgumentList();

            // xmlReader hold the first transformation
            XmlReader xmlReader = xslTransform.Transform(xmDocument, args);

            // ---------- i18n --------------------------
            var xsltI18nArgs = new XsltArgumentList();
            xsltI18nArgs.AddParam("lang", String.Empty, String.Empty);

            var xslt = new XslTransform();

            // Load the stylesheet.
            xslt.Load(Path.Combine(Home,I18N));

            xmlDoc = new XPathDocument(xmlReader);

            var writerFinal = new XmlTextWriter(
                fileName, System.Text.Encoding.GetEncoding("ISO-8859-1"));

            // Apply the second transform to xmlReader to final ouput
            xslt.Transform(xmlDoc, xsltI18nArgs, writerFinal);

            xmlReader.Close();
            writerFinal.Close();
        }

        #endregion Methods
    }
}