<?xml version="1.0" encoding="UTF-8"?>

<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
                xmlns:util="http://schemas.microsoft.com/wix/UtilExtension">
  <xsl:output omit-xml-declaration="yes" indent="yes" />

  <xsl:param name="InstallFolder" />
  <xsl:param name="DisplayName" />
  <xsl:param name="Name" />
  <xsl:param name="Description" />

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*" />
    </xsl:copy>
  </xsl:template>

  <!-- Set Directory Reference to INSTALLFOLDER (set if required) -->
  <xsl:template match="wix:DirectoryRef/@Id">
    <xsl:attribute name="Id">
      <xsl:value-of select="$InstallFolder" />
    </xsl:attribute>
  </xsl:template>

  <!-- XSL Template to inject WiX service installation elements to a .wxs generated from Heat Project task -->
  <!-- There may be other ways to look for your file -->
  <xsl:template match='wix:Wix/wix:Fragment/wix:DirectoryRef/wix:Component[wix:File[@Source="$(var.BasePath)\Bechtle.A365.ConfigService.Projection.exe"]]'>
    <xsl:element name="wix:Component">
      <xsl:attribute name="Id">
        <xsl:value-of select="@Id" />
      </xsl:attribute>

      <xsl:attribute name="Guid">
        <xsl:value-of select="@Guid" />
      </xsl:attribute>

      <xsl:element name="wix:File">
        <xsl:attribute name="Id">
          <xsl:value-of select="wix:File/@Id" />
        </xsl:attribute>

        <xsl:attribute name="Source">
          <xsl:value-of select="wix:File/@Source" />
        </xsl:attribute>

        <xsl:attribute name="KeyPath">yes</xsl:attribute>
      </xsl:element>

      <xsl:element name="wix:ServiceInstall">
        <!-- Service Install -->
        <xsl:attribute name="Id">SERVICEINSTALLER</xsl:attribute>

        <xsl:attribute name="DisplayName">
          <xsl:value-of select="$DisplayName" />
        </xsl:attribute>

        <xsl:attribute name="Name">
          <xsl:value-of select="$Name" />
        </xsl:attribute>

        <xsl:attribute name="Description">
          <xsl:value-of select="$Description" />
        </xsl:attribute>

        <xsl:attribute name="Start">auto</xsl:attribute>
        <xsl:attribute name="Type">ownProcess</xsl:attribute>
        <xsl:attribute name="Account">LocalSystem</xsl:attribute>
        <!--<xsl:attribute name="Password">[PASSWORD]</xsl:attribute>-->
        <xsl:attribute name="ErrorControl">normal</xsl:attribute>
        <xsl:attribute name="Vital">yes</xsl:attribute>

        <!-- Service Dependencies, if required -->
        <xsl:element name="wix:ServiceDependency">
          <xsl:attribute name="Id">DEPENDENCY</xsl:attribute>
        </xsl:element>

        <!-- Service Configuration, set as required -->
        <xsl:element name="util:ServiceConfig">
          <xsl:attribute name="FirstFailureActionType">restart</xsl:attribute>
          <xsl:attribute name="SecondFailureActionType">restart</xsl:attribute>
          <xsl:attribute name="ThirdFailureActionType">none</xsl:attribute>
          <xsl:attribute name="RestartServiceDelayInSeconds">60</xsl:attribute>
        </xsl:element>
      </xsl:element>

      <!-- Service Control, set as required -->
      <xsl:element name="wix:ServiceControl">
        <xsl:attribute name="Id">SERVICECONTROLLER</xsl:attribute>

        <xsl:attribute name="Name">
          <xsl:value-of select="$Name" />
        </xsl:attribute>

        <xsl:attribute name="Remove">uninstall</xsl:attribute>
        <xsl:attribute name="Stop">both</xsl:attribute>
        <xsl:attribute name="Start">install</xsl:attribute>
        <xsl:attribute name="Wait">yes</xsl:attribute>
      </xsl:element>
    </xsl:element>
  </xsl:template>
</xsl:stylesheet>