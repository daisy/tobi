<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xd="http://www.oxygenxml.com/ns/doc/xsl"
	xmlns:zai="http://www.daisy.org/ns/z3986/authoring/"
	xmlns:d="http://www.daisy.org/ns/z3986/authoring/features/description/"
	xmlns:xlink="http://www.w3.org/1999/xlink" exclude-result-prefixes="xd" version="1.0">

	<xsl:template match="d:description">
		<html xmlns="http://www.w3.org/1999/xhtml">
			<head>
				<style type="text/css">
					body {
						font-family : arial, sans-serif;
						font-size : 1em
					}
					h1 {
						font-size: 1.2em;
					}
					h2 {
						font-size: 1.1em;
						color: rgb(0,0,110)
					}
					h2.about {
						font-size: 1em;
						color: rgb(0,0,0)
					}
					div.container {
						border-top: solid 1px rgb(0,0,255);
						width: 80%;
						padding: 5px;
						margin-bottom: 10px;
						background-color: rgb(255,255,255)
					}
					div.about, div.access {
						font-size: 0.9em
					}
					div.annotation {
						font-size: 0.8em;
						font-weight: bold;
						width: 60%;
						border-top: 1px solid rgb(0,0,0)
					}
					p.anno-hd {
						color: rgb(0,0,110)
					}
					img {
						color: rgb(0,0,255)
					}
					ul {
						list-style-type: none
					}
					.center {
						text-align: center
					}
				</style>
			</head>
			<body>
				<h1>DIAGRAM Description</h1>
				
				<xsl:call-template name="about-this-description">
					<xsl:with-param name="meta" select="child::d:head"/>
				</xsl:call-template>
				
				<xsl:apply-templates select="child::d:body"/>
				
				<xsl:if test="//zai:meta[@property='dc:accessRights']">
					<xsl:element name="div">
						<xsl:attribute name="class">access center</xsl:attribute>
						<xsl:element name="div">
							<xsl:value-of select="//zai:meta[@property='dc:accessRights']"/>
						</xsl:element>
					</xsl:element>
				</xsl:if>
			</body>
		</html>
	</xsl:template>

	<xsl:template name="about-this-description">
		<xsl:param name="meta"/>
		
		<xsl:element name="div">
			<xsl:attribute name="class">container about</xsl:attribute>
			
			<xsl:element name="h2">
				<xsl:attribute name="class">about</xsl:attribute>
				<xsl:text>About this description</xsl:text>
			</xsl:element>
			
			<ul>
				<li><strong>Author:</strong>&#160;&#160;<xsl:value-of select="$meta/zai:meta[@property='dc:creator'][1]"/>, <xsl:value-of select="$meta/zai:meta[@property='diagram:credentials'][1]"/></li>
				<li><strong>Target Age:</strong> &#160;&#160;<xsl:value-of select="$meta/zai:meta[@property='diagram:targetAge']/@content"/></li>
				<li><strong>Target Grade:</strong>&#160;&#160;<xsl:value-of select="$meta/zai:meta[@property='diagram:targetGrade']/@content"/></li>
			</ul>
		</xsl:element>
	</xsl:template>

	<xsl:template match="*[parent::d:body][not(self::zai:annotation)]">
		<xsl:variable name="ename" select="local-name(.)"/>
		<xsl:element name="div">
			<xsl:attribute name="id"><xsl:value-of select="@xml:id"/></xsl:attribute>
			<xsl:attribute name="class">container</xsl:attribute>
			<xsl:element name="h2">
				<xsl:choose>
					<xsl:when test="$ename='summary'">Summary</xsl:when>
					<xsl:when test="$ename='longdesc'">Long Description</xsl:when>
					<xsl:when test="$ename='simplifiedLanguageDescription'">Simplified Language Description</xsl:when>
					<xsl:when test="$ename='tactile'">Tactile Image</xsl:when>
					<xsl:when test="$ename='simplifiedImage'">Simplified Image</xsl:when>
				</xsl:choose>
			</xsl:element>
			<xsl:apply-templates/>
			<xsl:if test="//zai:annotation[@ref=current()/@xml:id]">
				<xsl:element name="div">
					<xsl:attribute name="class">annotation</xsl:attribute>
					<p class="anno-hd">Annotation added by <xsl:value-of select="//zai:annotation[@ref=current()/@xml:id]/@by"/>:</p>
					<xsl:apply-templates select="//zai:annotation[@ref=current()/@xml:id][1]/*"/>
				</xsl:element>
			</xsl:if>
		</xsl:element>
	</xsl:template>

	<xsl:template match="zai:p">
		<xsl:element name="p">
			<xsl:apply-templates/>
		</xsl:element>
	</xsl:template>

	<xsl:template match="zai:object">
		<xsl:element name="img">
			<xsl:attribute name="src">
				<xsl:value-of select="@src"/>
			</xsl:attribute>
			<xsl:attribute name="srctype">
				<xsl:choose>
					<xsl:when test="contains(@src, '.svg')">image/svg+xml</xsl:when>
					<xsl:otherwise>unknown</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
			<xsl:attribute name="alt">
			<xsl:choose>
				<xsl:when test="parent::d:tactile">[Tactile image]</xsl:when>
				<xsl:otherwise>[Simplified image]</xsl:otherwise>
			</xsl:choose>
			</xsl:attribute>
		</xsl:element>
	</xsl:template>
	
	<xsl:template match="zai:annotation[parent::d:body]"/>
	
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

</xsl:stylesheet>
