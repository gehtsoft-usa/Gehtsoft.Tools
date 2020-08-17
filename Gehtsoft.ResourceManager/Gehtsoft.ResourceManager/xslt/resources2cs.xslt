<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="text" encoding="Windows-1252"/>
    <xsl:template match="component" >
using System;
using Gehtsoft.ResourceManager;

namespace <xsl:value-of select="./@namespace" />
{
    public static class <xsl:value-of select="./@name" />
    {
    <xsl:apply-templates select="./group" />
    <xsl:apply-templates select="./message" />
    }
}
    </xsl:template>
    <xsl:template match="group">
    <xsl:param name="prefix" />
    public static class <xsl:value-of select="./@id" />
    {
    <xsl:apply-templates select="./group" >
        <xsl:with-param name="prefix"><xsl:value-of select="$prefix" /><xsl:value-of select="./@id"/>.</xsl:with-param>
    </xsl:apply-templates>
    <xsl:apply-templates select="./message">
        <xsl:with-param name="prefix"><xsl:value-of select="$prefix" /><xsl:value-of select="./@id"/>.</xsl:with-param>
    </xsl:apply-templates>
    }
    </xsl:template>

    <xsl:template match="message">
    <xsl:param name="prefix" />
    <xsl:choose>
    <xsl:when test="./@format='yes'">
    public static string <xsl:value-of select="./@id" />(params object[] parameters) { return string.Format(ResourceManager.Messages["<xsl:value-of select="$prefix"/><xsl:value-of select="./@id" />"], parameters); }
    </xsl:when>
    <xsl:otherwise>
    public static string <xsl:value-of select="./@id" /> => ResourceManager.Messages["<xsl:value-of select="$prefix"/><xsl:value-of select="./@id" />"];

    </xsl:otherwise>
    </xsl:choose>
    </xsl:template>
</xsl:stylesheet>