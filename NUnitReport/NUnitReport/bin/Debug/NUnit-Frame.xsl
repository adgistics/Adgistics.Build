<?xml version="1.0" encoding="ISO-8859-1"?>

<xsl:stylesheet 
    version="2.0" 
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
    xmlns:nunit2report="urn:my-scripts">

<xsl:output method="html" indent="no" encoding="ISO-8859-1"/>

<xsl:param name="path.dir" />

<!-- 
  ====================================================================
  = Frame Set-up for Report
  ====================================================================
-->
<xsl:template name="index.html">
  <html>
    <head>
      <title>Unit Test Results.</title>
    </head>
    <frameset cols="25%,75%" framespacing="0" frameborder="1" border="2">
      <frameset rows="30%,70%" framespacing="0" frameborder="1" border="2">
        <frame src="assemblies-frame.html" name="packageListFrame"/>
        <frame src="classes-frame.html" name="classListFrame"/>
      </frameset>
      <frame src="overview-summary.html" name="classFrame"/>
      <noframes>
        <h2>Frame Alert</h2>
        <p>
        This document is designed to be viewed using the 
        frames feature. If you see this message, you are using
        a non-frame-capable web client.
        </p>
      </noframes>
    </frameset>
  </html>
</xsl:template>

<!-- 
  ====================================================================
  = Assemblies Report
  =
  = Creates a 'assemblies-frame.html' file that contains links to each
  = 'package-summary.html'.
  =
  = #BUG: There is a problem here, I don't know how to handle unnamed 
  =       package :(
  ====================================================================
-->
<xsl:template name="assemblies">
  <xsl:param name="assembly.name"/>
  <html>
    <head>
      <title>All Unit Test Packages</title>
      <xsl:call-template name="create.stylesheet.link"/>
    </head>
      <body style="padding-left:20px; padding-right:20px; padding-top:20px;">
        <h3>Assemblies</h3>
        <p class="side-menu">
        <a target="classFrame" href="overview-summary.html">
        <xsl:value-of select="$assembly.name"/>
        </a>
        </p>
    </body>
  </html>
</xsl:template>

<!-- 
  ====================================================================
  = Overview Report
  =
  = Creates a 'overview-summary.html' file that contains links to each
  = class report.
  ====================================================================
-->
<xsl:template name="overview">
  <html>
    <head>
      <title>Unit Test Results: Summary</title>
      <xsl:call-template name="create.stylesheet.link"/>
    </head>
    <body style="padding-left:40px; padding-right:40px;">
      <xsl:attribute name="onload">open('classes-frame.html','classListFrame')</xsl:attribute>
      <xsl:call-template name="page.header"/>

      <h2>Summary</h2>
      <xsl:variable name="runCount" select="@total"/>
      <xsl:variable name="failureCount" select="@failures"/>
      <xsl:variable name="ignoreCount" select="@not-run"/>
      <xsl:variable name="total" select="$runCount + $ignoreCount + $failureCount"/>
      <xsl:variable name="timeCount" select="translate(test-suite/@time,',','.')"/>
      <xsl:variable name="successRate" select="$runCount div $total"/>

      <table>
        <xsl:call-template name="summary.header"/>
        <tr valign="top">
          <xsl:attribute name="class">
            <xsl:choose>
              <xsl:when test="$failureCount &gt; 0">Failure</xsl:when>
              <xsl:when test="$ignoreCount &gt; 0">Error</xsl:when>
              <xsl:otherwise>Pass</xsl:otherwise>
            </xsl:choose>			
          </xsl:attribute>		
        <td><xsl:value-of select="$runCount"/></td>
        <td><xsl:value-of select="$failureCount"/></td>
        <td><xsl:value-of select="$ignoreCount"/></td>
        <td><xsl:value-of select="round($runCount div $total * 100)"/> %</td>
        <td  width="280px">
          <div style="position:relative; top:-7px;">
            <span class="covered"></span>
            <xsl:if test="round($ignoreCount * 100 div $total )!=0">
              <span class="ignored">
                <xsl:attribute name="style">width:<xsl:value-of select="round($ignoreCount * 100 div $total )"/>%</xsl:attribute>
              </span>
              </xsl:if>
              <xsl:if test="round($failureCount * 100 div $total )!=0">
                <span class="uncovered">
                  <xsl:attribute name="style">width:<xsl:value-of select="round($failureCount * 100 div $total )"/>%</xsl:attribute>
                </span>
            </xsl:if>
          </div>
        </td>
        <td>
          <xsl:call-template name="display-time">
            <xsl:with-param name="value" select="$timeCount"/>
          </xsl:call-template>
        </td>
        </tr>
    </table>
    <p>
    Note: <i>failures</i> are anticipated and checked for with 
    assertions while <i>errors</i>&#160; are unanticipated.
    </p>

    <h2>TestSuite Summary</h2>
    <table style="margin-bottom:40px;">
      <xsl:call-template name="assembly.summary.geader"/>
      <!-- list all packages recursively -->
      <xsl:for-each select="//test-suite[(child::results/test-case)]">
        <xsl:sort select="@name"/>

        <xsl:variable name="runCount2" select="count(child::results/test-case)"/>
        <xsl:variable name="errorCount2" select="count(child::results/test-case[@executed='False'])"/>
        <xsl:variable name="failureCount2" select="count(child::results/test-case[@success='False'])"/>
        <xsl:variable name="testCount2" select="$runCount2 + $errorCount2 + $failureCount2"/>
        <xsl:variable name="timeCount2" select="translate(@time,',','.')"/>

        <!-- write a summary for the package -->
        <tr valign="top">
          <!-- set a nice color depending if there is an error/failure -->
          <xsl:attribute name="class">
            <xsl:choose>
            <xsl:when test="$failureCount2 &gt; 0">Failure</xsl:when>
            <xsl:when test="$errorCount2 &gt; 0"> Error</xsl:when>
            <xsl:otherwise>Pass</xsl:otherwise>
            </xsl:choose>
          </xsl:attribute>
          
          <td>
            <xsl:variable name="path.dir">
              <xsl:for-each select="ancestor-or-self::*">
                <xsl:if test="not(contains(@name,'.dll')) and not(name()='results' or name()='testsummary')">
                <xsl:value-of select="concat(@name,'/')"/></xsl:if>
              </xsl:for-each>
            </xsl:variable>
            <a>
              <xsl:attribute name="href">
                <xsl:value-of select="$path.dir"/>
                <xsl:value-of select="@name"/>.html</xsl:attribute> 	
                <xsl:attribute name="class">
                <xsl:choose>
                  <xsl:when test="$failureCount2 &gt; 0">Failure</xsl:when>
                </xsl:choose>
              </xsl:attribute> 	
              <xsl:value-of select="@name"/>
            </a>
        </td>
        <td width="15%" align="right">
          <xsl:variable name="successRate2" select="$runCount2 div $testCount2"/>
          <xsl:value-of select="round($runCount2 div $testCount2 * 100)"/> %
        </td>
        <td width="40%" height="9px">
          <div style="position:relative; top:-7px;">
            <span class="covered"></span>
            <xsl:if test="round($errorCount2 * 100 div $testCount2 )!=0">
              <span class="ignored">
                <xsl:attribute name="style">width:<xsl:value-of select="round($errorCount2 * 100 div $testCount2 )"/>%</xsl:attribute>
              </span>
            </xsl:if>
            <xsl:if test="round($failureCount2 * 100 div $testCount2 )!=0">
              <span class="uncovered">
                <xsl:attribute name="style">width:<xsl:value-of select="round($failureCount2 * 100 div $testCount2 )"/>%</xsl:attribute>
              </span>
            </xsl:if>
          </div>
        </td>
        <td><xsl:value-of select="$runCount2"/></td>
        <td><xsl:value-of select="$errorCount2"/></td>
        <td><xsl:value-of select="$failureCount2"/></td>
        <td>
          <xsl:call-template name="display-time">
            <xsl:with-param name="value" select="$timeCount2"/>
          </xsl:call-template>				
        </td>					
        </tr>
      </xsl:for-each>
    </table>		

    </body>
  </html>
</xsl:template>

<!-- 
  ====================================================================
  = All Classes Menu
  =
  = Creates an 'all-classes.html' file that contains a link
  = to each class reprt.
  ====================================================================
-->
<xsl:template name="classes" >
    <html>
        <head>
            <title>All Unit Test Classes</title>
            <xsl:call-template name="create.stylesheet.link"/>
        </head>
        <body style="padding-left:20px; padding-right:20px; padding-top:20px;">
          <h3>Test Suite</h3>
            <!-- list all packages recursively -->
            <xsl:for-each select="//test-suite[(child::results/test-case)]">
              <xsl:sort select="@name"/>

              <xsl:variable 
                  name="errorCount" 
                  select="count(child::results/test-case[@executed='False'])"/>
              <xsl:variable 
                  name="failureCount" 
                  select="count(child::results/test-case[@success='False'])"/>

              <xsl:variable name="path.dir">
                <xsl:for-each select="ancestor-or-self::*">
                  <xsl:if test="not(contains(@name,'.dll')) and not(name()='results' or name()='testsummary')">
                    <xsl:value-of select="concat(@name,'/')"/>
                  </xsl:if>
                </xsl:for-each>
              </xsl:variable>
              
              <!-- write a summary for the package  -->
              <p class="side-menu">
                <a target="classFrame" >
                  <xsl:attribute name="href">
                    <xsl:value-of select="$path.dir"/><xsl:value-of select="@name"/>.html
                  </xsl:attribute> 	
                  <xsl:value-of select="@name"/>
                </a>
              </p>
            </xsl:for-each>

        </body>
    </html>
</xsl:template>

<!-- 
  ====================================================================
  = Create The Link To The SytleSheet
  ====================================================================
-->
<xsl:template name="create.stylesheet.link">
    <link rel="stylesheet" type="text/css" title="Style">
      <xsl:attribute name="href">/stylesheet.css</xsl:attribute>
    </link>
</xsl:template>

<!-- 
  ====================================================================
  = Page Header
  ====================================================================
-->
<xsl:template name="page.header">
  <h1>Unit Tests Results</h1>
	<hr size="1"/>
</xsl:template>

<!-- 
  ====================================================================
  = Summary Header
  ====================================================================
-->
<xsl:template name="summary.header">
	<tr valign="top" class="TableHeader">
		<td>Tests</td>
		<td>Failures</td>
		<td>Errors</td>
		<td colspan="2">Success Rate</td>
		<td>Time(s)</td>
	</tr>
</xsl:template>

<!-- 
  ====================================================================
  = Test Case Report
  ====================================================================
-->
<xsl:template name="test.case">

	<xsl:param name="dir.test"/>
	<xsl:param name="summary.xml"/>
	<xsl:param name="open.description"/>

	<xsl:variable name="summaries" select="document($summary.xml)" />

    <html>
        <head>
            <title>Unit Test for class <xsl:value-of select="./@name"/></title>
            <xsl:call-template name="create.stylesheet.link"/>
        </head>
        <body style="padding-left:40px; padding-right:40px;">
        <xsl:call-template name="page.header"/>
			
			<h3>Test Suite</h3>

			<!-- Summary -->
			<table>
					<xsl:call-template name="assembly.summary.geader"/>

					<xsl:variable name="testCount" select="count(./results/test-case)"/>
					<xsl:variable name="errorCount" select="count(./results/test-case[@executed='False'])"/>
					<xsl:variable name="failureCount" select="count(./results/test-case[@success='False'])"/>
					<xsl:variable name="runCount" select="$testCount - $errorCount - $failureCount"/>
					<xsl:variable name="timeCount" select="translate(@time,',','.')"/>

					<!-- write a summary for the package -->
					<tr valign="top">
						<!-- set a color depending if there is an error/failure -->
						<xsl:attribute name="class">
							<xsl:choose>
								<xsl:when test="$failureCount &gt; 0">Failure</xsl:when>
								<xsl:when test="$errorCount &gt; 0"> Error</xsl:when>
								<xsl:otherwise>Pass</xsl:otherwise>
							</xsl:choose>
						</xsl:attribute> 	
						<td>
							<xsl:value-of select="@name"/>
						</td>
					<td width="15%" align="right">
						<xsl:value-of select="round($runCount div $testCount * 100)"/> %
					</td>
					<td width="40%" height="9px">
            <div style="position:relative; top:-7px;">
              <span class="covered"></span>
              <xsl:if test="round($errorCount * 100 div $testCount )!=0">
                <span class="ignored">
                  <xsl:attribute name="style">width:<xsl:value-of select="round( ($errorCount * 100 div $testCount) + ($errorCount * 100 div $testCount) )"/>%</xsl:attribute>
                </span>
              </xsl:if>
              <xsl:if test="round($failureCount * 100 div $testCount )!=0">
                <span class="uncovered">
                  <xsl:if test="round($errorCount * 100 div $testCount )!=0">
                    <xsl:attribute name="style">left:<xsl:value-of select="round($failureCount * 100 div $testCount )"/>%;</xsl:attribute>
                  </xsl:if>
                  <xsl:attribute name="style">width:<xsl:value-of select="round($failureCount * 100 div $testCount )"/>%;</xsl:attribute>
                </span>
              </xsl:if>
              </div>
					</td>
						<td><xsl:value-of select="$runCount"/>
						</td>
						<td><xsl:value-of select="$errorCount"/></td>
						<td><xsl:value-of select="$failureCount"/></td>
						<td>
						   <xsl:call-template name="display-time">
								<xsl:with-param name="value" select="$timeCount"/>
							</xsl:call-template>				
						</td>		
					</tr>
			</table>
			<br/>
			<h3>Test Case</h3>
			<table style="margin-bottom:40px;">
			<!-- Header -->
			<xsl:call-template name="classes.summary.header"/>

			<!-- match the testcases of this package -->
			<xsl:for-each select="child::results/test-case">
				<xsl:sort select="@name"/>
				<xsl:variable name="newid" select="generate-id(@name)" />
				<xsl:variable name="Mname" select="concat('M:',@name)" />

			   <xsl:variable name="result">
						<xsl:choose>
						  <xsl:when test="./failure">Failure</xsl:when>
							<xsl:when test="./error">Error</xsl:when>
							<xsl:when test="@executed='False'">Ignored</xsl:when>
							<xsl:otherwise>Pass</xsl:otherwise>
						</xsl:choose>
			   </xsl:variable>

				<tr valign="top">
					<xsl:attribute name="class"><xsl:value-of select="$result"/></xsl:attribute>
					<td>
						<xsl:choose>
              <xsl:when test="$result = 'Failure' or $result = 'Error'">
                <a style="text-decoration:none">
                <xsl:attribute name="class">Failure</xsl:attribute>
                <xsl:value-of select="./@name"/>
                </a>
              </xsl:when>
              <xsl:when test="$result = 'Ignored'">
                <a>
                <xsl:attribute name="class">ignored</xsl:attribute>
                <xsl:value-of select="./@name"/>
                </a>
              </xsl:when>
              <xsl:otherwise>
                <xsl:attribute name="class">method</xsl:attribute>
                <xsl:value-of select="./@name"/>
              </xsl:otherwise>
						</xsl:choose>
					</td>
					<td>
            <xsl:value-of select="$result"/>
          </td>
					<td>
						<xsl:call-template name="display-time">
							<xsl:with-param name="value" select="@time"/>
						</xsl:call-template>				
					</td>
				</tr>
				<xsl:if test="$result != &quot;Pass&quot;">
					<tr style="background:none;">
						<td class="FailureDetail" style="border:none;">
						<div>
						<p class="errorbox">
							<xsl:apply-templates select="./failure"/>
							<xsl:apply-templates select="./error"/>
							<xsl:apply-templates select="./reason"/>
						</p>
						</div>
						</td>	      
					</tr>
				</xsl:if>
			</xsl:for-each>
			</table>

		</body>
	</html>
</xsl:template>

<!-- 
  ====================================================================
  = Package Summary Header
  ====================================================================
-->
<xsl:template name="assembly.summary.geader">
	<tr class="TableHeader" valign="top">
		<td width="60%" colspan="3">Name</td>
		<td width="10%">Tests</td>
		<td width="10%">Errors</td>
		<td width="10%">Failures</td>
		<td width="10%" nowrap="nowrap">Time(s)</td>
	</tr>
</xsl:template>

<!-- 
  ====================================================================
  = Classes Summary Header
  ====================================================================
-->
<xsl:template name="classes.summary.header">
	<tr class="TableHeader" valign="top">
		<td width="85%">Name</td>
		<td width="10%">Status</td>
		<td width="5%" nowrap="nowrap">Time(s)</td>
	</tr>
</xsl:template>

<!-- 
  ====================================================================
  = Format a 'value' as a time value 
  ====================================================================
-->
<xsl:template name="display-time">
	<xsl:param name="value"/>
	<xsl:value-of select="format-number($value,'0.000')"/>
</xsl:template>

<!-- 
  ====================================================================
  = CSS Document
  ====================================================================
-->
<xsl:template name="stylesheet.css">
body
{
  background-color: #F8F8F8;
  background-color: #F8F8F8;
  color: #444444;
  font: 15px/25px "museo-sans", "Lucida Grande", Lucida, Verdana, sans-serif;
  text-align: left;
}
span.covered {
	background: rgb(83, 213, 83); 
	border:#9c9c9c 1px solid;
	display:block;
  height:16px;
  -moz-border-radius: 8px;
  border-radius: 8px;
  position:absolute; 
  top:0; 
  left:0;
  width:100%;
  box-shadow: inset 2px -2px 6px black, inset 0px 2px 6px whitesmoke
}
span.uncovered {
	background: #df0000; 
	display:block;
	height:16px;
  -moz-border-radius: 8px;
  border-radius: 8px;
  position:absolute; 
  top:0; 
  left:0;
  box-shadow: inset 2px -2px 6px black, inset 0px 2px 6px whitesmoke
}
span.ignored {
	background: #ffff00;
	height:16px;
  -moz-border-radius: 8px;
  border-radius: 8px;
  position:absolute; 
  top:0; 
  left:0;
  box-shadow: inset 2px -2px 6px black, inset 0px 2px 6px whitesmoke
}
table, tr, td {
	border: #dcdcdc 1px solid;
	border-spacing:0px;
}
table {
	width: 100%;
}
td {
	height: 40px;
	padding:0 10 0 10;
  vertical-align:middle;
}
p {
	line-height:1.5em;
	margin-top:0.5em; 
	margin-bottom:1.0em;
}
p.errorbox{
  font-family: monospace;
  overflow-x: scroll; 
  background-color:LightGrey; 
  border-radius: 10px; padding:10px; 
  box-shadow: 0px 5px 5px #888; 
  border: #888 solid 1;
  margin:10px;
}
body, div, ul, ol, li,
h1, h2, h3, h4, h5, h6,
pre, code, p, dt, dd {
  margin: 0;
  padding: 0;
  line-height: 1.5em;
}
h1, h2, h3, h4, h5, h6, dt {
  font-size: 100%;
  font-weight: normal;
  font-weight: bold;
  color: #932277;
  background-color: inherit;
}
h1 {
  font-size: 185%;
  padding-bottom: 15px;
  padding-top: 45px;
}
h2 {
  font-size: 160%;
  padding-bottom: 15px;
  padding-top: 45px;
}
h3 {
  font-size: 120%;
  padding-bottom: 5px;
  padding-top: 25px;
  color: #444444;
}
dt {
  font-size: 100%;
  color: #444444;
}
h4 {
  font-size: 110%;
  padding-bottom: 3px;
  padding-top: 25px;
  color: #444444;
}
h5 {
  font-size: 105%;
  padding-top: 25px;
  color: #444444;
}
h6 {
  font-size: 100%;
  padding-top: 25px;
  color: #444444;
}
p, dd {
  line-height: 1.3em;
  margin-bottom: 0.5em;
  padding-left: 5px;
}
p.side-menu {
  font-size: 90%;
}
ul, ol {
  list-style-position: outside;
  padding-bottom: 1.0em;
}
li {
  margin-left: 40px;
  padding-bottom: 0px;
}
a {
  text-decoration: none;
}
a:link, a:visited {
  color: #932277;
  background-color: inherit;
}
a:hover, a:active {
  text-decoration: underline;
  color: #932277;
}
.Error {
	font-weight:bold; 
}
.Failure {
	font-weight:bold; 
	color:#df0000;
}
.Ignored {
	font-weight:bold; 
}
.FailureDetail {
	font-size: -1;
	padding-left: 2.0em;
	background:none;
}
.Pass {
	padding-left:2px;
}
.TableHeader {
	background: #efefef;
	color: #000;
	font-weight: bold;
	horizontal-align: center;
}
.description {
	margin-top:1px;
	padding:3px;
	background-color:#dcdcdc;
	color:#000;
	font-weight:normal;
}
.method{
	color:#000;
	font-weight:normal;
	padding-left:5px;
}
a.Failure {
	font-weight:bold; 
	color:#df0000;
	text-decoration: none;
}
a.Failure:visited {
	font-weight:bold; 
	color:#df0000;
	text-decoration: none;
}
a.Failure:active {
	font-weight:bold; 
	color:#df0000;
	text-decoration: none;
}
a.error {
	font-weight:bold; 
	color:#df0000;
}
a.error:visited {
	font-weight:bold; 
	color:#df0000;
}
a.error:active {
	font-weight:bold; 
	color:#df0000;
}
a.ignored {
	font-weight:bold; 
	text-decoration: none;
	padding-left:5px;
}
a.ignored:visited {
	font-weight:bold; 
	text-decoration: none;
	padding-left:5px;
}
a.ignored:active {
	font-weight:bold; 
	text-decoration: none;
	padding-left:5px;
}
</xsl:template>

</xsl:stylesheet>