<?xml version="1.0" encoding="UTF-8"?>
<!--
	untitled
	Created by Romain Deltour on 2008-09-16.
	Copyright (c) 2008 DAISY Consortium. All rights reserved.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="http://www.w3.org/1999/xhtml">
	
	<xsl:output encoding="utf-8" indent="yes" method="xml" doctype-system="http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd" doctype-public="-//W3C//DTD XHTML 1.1//EN"/>
	
	<xsl:param name="server"/>
	<xsl:param name="modulename"/>
	<xsl:param name="pagename"/>
	<xsl:param name="spacereplace"/>
	<xsl:param name="slashreplace"/>
	
	<xsl:param name="title" select="//h1[1]"/>

	<xsl:template name="stringReplace">
		<xsl:param name="string" />
		<xsl:param name="from" />
		<xsl:param name="to" />
		<xsl:choose>
		<xsl:when test="contains($string, $from)">
			<xsl:value-of select="substring-before($string, $from)" />
			<xsl:value-of select="$to" />
			<xsl:call-template name="stringReplace">
				<xsl:with-param name="string">
					<xsl:value-of select="substring-after($string, $from)" />
				</xsl:with-param>
				<xsl:with-param name="from">
					<xsl:value-of select="$from" />
				</xsl:with-param>
				<xsl:with-param name="to">
					<xsl:value-of select="$to" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:when>
		<xsl:otherwise>
			<xsl:value-of select="$string" />
		</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="/">
		<xsl:message><xsl:value-of select="$pagename"/></xsl:message>
		<xsl:message><xsl:value-of select="$modulename"/></xsl:message>
		<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
			<head>
				<title><xsl:value-of select="$title"/></title>
		      <!-- xsl:element name="base">
					<xsl:attribute name="href"><xsl:value-of select="$server"/></xsl:attribute>
				</xsl:element -->
			</head>
			<body>
				<xsl:apply-templates select="/html/body/*" />
			</body>
		</html>
	</xsl:template>
	
	<xsl:template match="//a[@class='wiki']">
		<xsl:variable name="hreff">
			<xsl:call-template name="stringReplace">
				<xsl:with-param name="string">
					<xsl:value-of select="substring-after(@href,'wiki/')" />
				</xsl:with-param>
				<xsl:with-param name="from">
					<xsl:value-of select="'/'" />
				</xsl:with-param>
				<xsl:with-param name="to">
					<xsl:value-of select="$slashreplace" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="hrefx">
			<xsl:call-template name="stringReplace">
				<xsl:with-param name="string">
					<xsl:value-of select="$hreff" />
				</xsl:with-param>
				<xsl:with-param name="from">
					<xsl:value-of select="' '" />
				</xsl:with-param>
				<xsl:with-param name="to">
					<xsl:value-of select="$spacereplace" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:variable>
		<xsl:element name="a">
			<xsl:attribute name="href"><xsl:value-of select="$hrefx"/>.html</xsl:attribute>
			<xsl:attribute name="class">wiki</xsl:attribute>
			<xsl:value-of select="." />
		</xsl:element>
	</xsl:template>

	<xsl:template match="//a">
		
	<xsl:variable name="prefix"><xsl:value-of select="$modulename" />/wiki/</xsl:variable>
	
	<xsl:choose>
	<xsl:when test="contains(@href, $prefix)">

		<xsl:variable name="hreff">
			<xsl:call-template name="stringReplace">
				<xsl:with-param name="string">
					<xsl:value-of select="substring-after(@href,$prefix)" />
				</xsl:with-param>
				<xsl:with-param name="from">
					<xsl:value-of select="'/'" />
				</xsl:with-param>
				<xsl:with-param name="to">
					<xsl:value-of select="$slashreplace" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="hrefx">
			<xsl:call-template name="stringReplace">
				<xsl:with-param name="string">
					<xsl:value-of select="$hreff" />
				</xsl:with-param>
				<xsl:with-param name="from">
					<xsl:value-of select="' '" />
				</xsl:with-param>
				<xsl:with-param name="to">
					<xsl:value-of select="$spacereplace" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:variable>
		<xsl:element name="a">
			<xsl:attribute name="href"><xsl:value-of select="$hrefx"/>.html</xsl:attribute>
			<xsl:attribute name="title"><xsl:value-of select="@title"/></xsl:attribute>
			<xsl:value-of select="." />
		</xsl:element>
	</xsl:when>
	
	<xsl:when test="starts-with(@href, '/')">
		<xsl:element name="a">
			<xsl:attribute name="href"><xsl:value-of select="$server"/><xsl:value-of select="@href"/></xsl:attribute>
			<xsl:attribute name="title"><xsl:value-of select="@title"/></xsl:attribute>
			<xsl:value-of select="." />
		</xsl:element>
	</xsl:when>
	
	<xsl:otherwise>
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:otherwise>
	</xsl:choose>
	</xsl:template>


	<xsl:template name="writeImage">
		<xsl:param name="imgname" />
		
		<xsl:element name="img">
			<xsl:variable name="src">
				<xsl:call-template name="stringReplace">
					<xsl:with-param name="string">
						<xsl:value-of select="$imgname" />
					</xsl:with-param>
					<xsl:with-param name="from">
						<xsl:value-of select="' '" />
					</xsl:with-param>
					<xsl:with-param name="to">
						<xsl:value-of select="$spacereplace" />
					</xsl:with-param>
				</xsl:call-template>
			</xsl:variable>
			
			<xsl:attribute name="src"><xsl:value-of select="$pagename"/><xsl:value-of select="$slashreplace"/><xsl:value-of select="$src"/></xsl:attribute>
			<xsl:attribute name="alt"><xsl:value-of select="substring-before($imgname,'.')"/></xsl:attribute>

		</xsl:element>
	</xsl:template>

	<xsl:template match="div[@class='system-message']">
		<xsl:variable name="imgnamex" select="substring-before(substring-after(strong,'Image('),') failed')"/>
		
		<xsl:choose>
		<xsl:when test="substring($imgnamex, (string-length($imgnamex) - string-length('%')) + 1) = '%'">
			<xsl:call-template name="writeImage">
				<xsl:with-param name="imgname">
					<xsl:value-of select="substring-before($imgnamex,',')" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:when>
		<xsl:otherwise>
			<xsl:call-template name="writeImage">
				<xsl:with-param name="imgname">
					<xsl:value-of select="$imgnamex" />
				</xsl:with-param>
			</xsl:call-template>
		</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="p[div]">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="p[ul]">
		<xsl:apply-templates/>
	</xsl:template>
	
	<xsl:template match="li[following-sibling::*[1][self::ol]]">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
			<xsl:apply-templates select="following-sibling::*[1]" mode="copy"/>			
		</xsl:copy>
	</xsl:template>	
	<xsl:template match="ol[preceding-sibling::*[1][self::li]]">
	</xsl:template>
	<xsl:template match="ol[preceding-sibling::*[1][self::li]]" mode="copy">
				<xsl:call-template name="copy"/>
	</xsl:template>
	
	<!-- Add a table of contents -->
  <xsl:template match="//div[@class='wiki-toc']">
    <xsl:element name="div">
      <xsl:attribute name="id">toc</xsl:attribute>
	   <xsl:attribute name="class">toc</xsl:attribute>
      <xsl:element name="h2">
			<xsl:attribute name="id">toctitle</xsl:attribute>
			<xsl:text>Table of Contents</xsl:text>
		</xsl:element>
      <xsl:element name="ul">
        <xsl:apply-templates select="//h2[not(@id='toctitle')]" mode="toc"/>
      </xsl:element>
    </xsl:element>
  </xsl:template>

  <xsl:template match="*" mode="toc-a">
    <xsl:element name="a">
      <xsl:attribute name="href">
        <xsl:choose>
          <xsl:when test="@id">
            <xsl:value-of select="concat('#',@id)"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="concat('#',generate-id())"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:apply-templates/>
    </xsl:element>
  </xsl:template>

  <xsl:template match="h2" mode="toc">
    <xsl:element name="li">
      <xsl:apply-templates select="." mode="toc-a"/>
      <xsl:if test="following::*[self::h2 or self::h3][1][self::h3]">
        <xsl:element name="ul">
          <xsl:apply-templates select="following::h3[generate-id(preceding::h2[1])=generate-id(current())]" mode="toc"/>
        </xsl:element>
      </xsl:if>
    </xsl:element>
  </xsl:template>

  <xsl:template match="h3" mode="toc">
    <xsl:element name="li">
      <xsl:apply-templates select="." mode="toc-a"/>
      <xsl:if test="following::*[self::h3 or self::h4][1][self::h4]">
        <xsl:element name="ul">
          <xsl:apply-templates select="following::h4[generate-id(preceding::h3[1])=generate-id(current())]" mode="toc"/>
        </xsl:element>
      </xsl:if>
    </xsl:element>
  </xsl:template>

  <xsl:template match="h4" mode="toc">
    <xsl:element name="li">
      <xsl:apply-templates select="." mode="toc-a"/>
    </xsl:element>
  </xsl:template>

	<!-- Identity copying with introduction of the XHTML namespace -->
	<xsl:template match="*|comment()|processing-instruction()">
		<xsl:element name="{local-name()}" namespace="http://www.w3.org/1999/xhtml">
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:element>
	</xsl:template>
	
</xsl:stylesheet>
