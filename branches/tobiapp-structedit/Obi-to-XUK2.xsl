<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.daisy.org/urakawa/xuk/2.0"
    xmlns:xuk1="http://www.daisy.org/urakawa/xuk/1.0"
    xmlns:xuk2="http://www.daisy.org/urakawa/xuk/2.0" xmlns:obi="http://www.daisy.org/urakawa/obi"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    exclude-result-prefixes="xs" version="2.0">
  <xsl:output method="xml" indent="yes" omit-xml-declaration="no" version="1.0"/>
  <!-- xsl:strip-space elements="*"/ -->
  <!-- xsl:template match="text()[not(normalize-space())]"/ -->
  <xsl:template match="text()"/>

  <!--
        This XUK transformation works on these 3 types of XML documents:
        (1) OBI_XUK1 => used by old Obi up to v1.2 (included), based on Urakawa SDK 1.1
        (2) OBI_XUK2 => used by new Obi from v2.0 (under development), based on Urakawa SDK 2, with custom data model extensions and specific node structure (no use of XmlProperty).
        (3) TOBI_XUK2 => used by all versions of Tobi, based on Urakawa SDK 2, without data model extensions, generic tree nodes but with full use of XmlProperty to represent document markup.
        This XUK transformation can convert the following:
        (1) OBI_XUK1 to OBI_XUK2 (upgrade old Obi projects to new ones)
        (2) OBI_XUK1 to TOBI_XUK2 (repurpose old Obi projects into full-text, full-audio Tobi projects)
        (3) OBI_XUK2 to TOBI_XUK2 (repurpose new Obi projects into full-text, full-audio Tobi projects)
        Note that there is no reverse transformation from the Tobi format to the Obi formats,
        or from the new Obi format to the old one.
        The choice of (1) or (2) is controlled by $generateObiFormat [false => (2), true => (1)].
        Option (3) implies $generateObiFormat = false, and is automatically used when OBI_XUK2 is detected.
    -->
  <xsl:variable name="generateObiFormat" as="xs:boolean" select="false()"/>
  <xsl:variable name="generateTobiFormat" as="xs:boolean" select="not($generateObiFormat)"/>

  <!-- MAIN ENTRY POINT -->
  <xsl:template match="/">
    <xsl:apply-templates/>
  </xsl:template>

  <!-- XUK root -->
  <xsl:template match="xuk1:Xuk | xuk2:Xuk">
    <Xuk>
      <xsl:namespace name="xsi">
        <xsl:text>http://www.w3.org/2001/XMLSchema-instance</xsl:text>
      </xsl:namespace>
      <xsl:attribute name="noNamespaceSchemaLocation"
          namespace="http://www.w3.org/2001/XMLSchema-instance">
        <xsl:text>http://www.daisy.org/urakawa/xuk/2.0/xuk.xsd</xsl:text>
      </xsl:attribute>
      <xsl:apply-templates/>
    </Xuk>
  </xsl:template>

  <!-- Project root -->
  <xsl:template match="xuk1:Project | xuk2:Project">
    <Project>
      <PresentationFactory>
        <RegisteredTypes>
          <Type XukLocalName="Presentation"
              XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
              AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
              FullName="urakawa.Presentation"/>
          <Type XukLocalName="ObiPresentation"
              XukNamespaceUri="http://www.daisy.org/urakawa/obi"
              BaseXukLocalName="Presentation"
              BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
              AssemblyName="Obi" AssemblyVersion="0.0.0.0" FullName="Obi.ObiPresentation"
                    />
        </RegisteredTypes>
      </PresentationFactory>
      <xsl:apply-templates/>
    </Project>
  </xsl:template>

  <!-- List of Presentations -->
  <xsl:template match="xuk1:mPresentations | xuk2:Presentations">
    <Presentations>
      <xsl:apply-templates/>
    </Presentations>
  </xsl:template>

  <!-- Presentation factories and managers (full warm-up of all the XukAble types) -->
  <xsl:template name="PresentationFactoriesAndManagers">
    <TreeNodeFactory>
      <RegisteredTypes>
        <Type XukLocalName="TreeNode" XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.core.TreeNode"/>
        <Type XukLocalName="root" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
            BaseXukLocalName="TreeNode"
            BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0" AssemblyName="Obi"
            AssemblyVersion="0.0.0.0" FullName="Obi.ObiRootNode"/>
        <Type XukLocalName="section" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
            BaseXukLocalName="TreeNode"
            BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0" AssemblyName="Obi"
            AssemblyVersion="0.0.0.0" FullName="Obi.SectionNode"/>
        <Type XukLocalName="phrase" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
            BaseXukLocalName="TreeNode"
            BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0" AssemblyName="Obi"
            AssemblyVersion="0.0.0.0" FullName="Obi.PhraseNode"/>
        <Type XukLocalName="empty" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
            BaseXukLocalName="TreeNode"
            BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0" AssemblyName="Obi"
            AssemblyVersion="0.0.0.0" FullName="Obi.EmptyNode"/>
      </RegisteredTypes>
    </TreeNodeFactory>
    <PropertyFactory DefaultXmlNamespaceUri="http://www.daisy.org/z3986/2005/dtbook/">
      <RegisteredTypes>
        <Type XukLocalName="ChannelsProperty"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.property.channel.ChannelsProperty"/>
        <Type XukLocalName="XmlProperty"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.property.xml.XmlProperty"/>
      </RegisteredTypes>
    </PropertyFactory>
    <ChannelFactory>
      <RegisteredTypes>
        <Type XukLocalName="Channel" XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.property.channel.Channel"/>
        <Type XukLocalName="TextChannel"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.property.channel.TextChannel"/>
        <Type XukLocalName="ImageChannel"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.property.channel.ImageChannel"/>
        <Type XukLocalName="AudioChannel"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.property.channel.AudioChannel"/>
      </RegisteredTypes>
    </ChannelFactory>
    <MediaFactory>
      <RegisteredTypes>
        <Type XukLocalName="ManagedImageMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.data.image.ManagedImageMedia"/>
        <Type XukLocalName="ManagedAudioMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.data.audio.ManagedAudioMedia"/>
        <Type XukLocalName="TextMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.TextMedia"/>
        <Type XukLocalName="ExternalImageMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.ExternalImageMedia"/>
        <Type XukLocalName="ExternalVideoMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.ExternalVideoMedia"/>
        <Type XukLocalName="ExternalTextMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.ExternalTextMedia"/>
        <Type XukLocalName="ExternalAudioMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.ExternalAudioMedia"/>
        <Type XukLocalName="SequenceMedia"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.SequenceMedia"/>
      </RegisteredTypes>
    </MediaFactory>
    <DataProviderFactory>
      <RegisteredTypes>
        <Type XukLocalName="FileDataProvider"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.data.FileDataProvider"/>
      </RegisteredTypes>
    </DataProviderFactory>
    <MediaDataFactory>
      <RegisteredTypes>
        <Type XukLocalName="JpgImageMediaData"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.data.image.codec.JpgImageMediaData"/>
        <Type XukLocalName="WavAudioMediaData"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.media.data.audio.codec.WavAudioMediaData"/>
      </RegisteredTypes>
    </MediaDataFactory>
    <CommandFactory>
      <RegisteredTypes>
        <Type XukLocalName="CompositeCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.command.CompositeCommand"/>
        <Type XukLocalName="TreeNodeSetManagedAudioMediaCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.TreeNodeSetManagedAudioMediaCommand"/>
        <Type XukLocalName="ManagedAudioMediaInsertDataCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.ManagedAudioMediaInsertDataCommand"/>
        <Type XukLocalName="TreeNodeAudioStreamDeleteCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.TreeNodeAudioStreamDeleteCommand"/>
        <Type XukLocalName="TreeNodeSetIsMarkedCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.TreeNodeSetIsMarkedCommand"/>
        <Type XukLocalName="TreeNodeChangeTextCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.TreeNodeChangeTextCommand"/>
        <Type XukLocalName="MetadataAddCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.MetadataAddCommand"/>
        <Type XukLocalName="MetadataRemoveCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.MetadataRemoveCommand"/>
        <Type XukLocalName="MetadataSetContentCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.MetadataSetContentCommand"/>
        <Type XukLocalName="MetadataSetNameCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.MetadataSetNameCommand"/>
        <Type XukLocalName="MetadataSetIdCommand"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.commands.MetadataSetIdCommand"/>
      </RegisteredTypes>
    </CommandFactory>
    <MetadataFactory>
      <RegisteredTypes>
        <Type XukLocalName="Metadata" XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.metadata.Metadata"/>
      </RegisteredTypes>
    </MetadataFactory>
    <ExternalFileDataFactory>
      <RegisteredTypes>
        <Type XukLocalName="CssExternalFileData"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.ExternalFiles.CSSExternalFileData"/>
        <Type XukLocalName="XsltExternalFileData"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.ExternalFiles.XSLTExternalFileData"/>
        <Type XukLocalName="DTDExternalFileData"
            XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
            AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
            FullName="urakawa.ExternalFiles.DTDExternalFileData"/>
      </RegisteredTypes>
    </ExternalFileDataFactory>
    <ExternalFileDataManager>
      <ExternalFileDatas/>
    </ExternalFileDataManager>
  </xsl:template>

  <!-- ChannelsManager -->
  <xsl:template match="xuk2:ChannelsManager">
    <xsl:param tunnel="yes" name="ChannelID_audio">ChannelID_audio</xsl:param>
    <xsl:param tunnel="yes" name="ChannelID_text">ChannelID_text</xsl:param>
    <ChannelsManager>
      <Channels>
        <!-- CH00001-->
        <AudioChannel Uid="{$ChannelID_audio}" Name="The Audio Channel"/>
        <!-- CH00000-->
        <TextChannel Uid="{$ChannelID_text}" Name="The Text Channel"/>
        <ImageChannel Uid="CH00002" Name="The Image Channel"/>
      </Channels>
    </ChannelsManager>
  </xsl:template>

  <!-- ChannelsManager -->
  <xsl:template match="xuk1:ChannelsManager">
    <xsl:param tunnel="yes" name="ChannelID_audio">ChannelID_audio</xsl:param>
    <xsl:param tunnel="yes" name="ChannelID_text">ChannelID_text</xsl:param>
    <ChannelsManager>
      <Channels>
        <!-- CHID0000-->
        <AudioChannel Uid="{$ChannelID_audio}" Name="The Audio Channel"/>
        <!-- CHID0001-->
        <TextChannel Uid="{$ChannelID_text}" Name="The Text Channel"/>
        <ImageChannel Uid="CHID0002" Name="The Image Channel"/>
      </Channels>
    </ChannelsManager>
  </xsl:template>

  <!-- Presentation root -->
  <xsl:template match="obi:Presentation | obi:ObiPresentation">
    <xsl:choose>
      <xsl:when test="$generateObiFormat">
        <obi:ObiPresentation>
          <xsl:attribute name="RootUri">
            <xsl:text>./</xsl:text>
          </xsl:attribute>
          <xsl:call-template name="PresentationFactoriesAndManagers"/>

          <xsl:choose>
		    <xsl:when test="./local-name() = 'Presentation'">
              <xsl:apply-templates>
                <xsl:with-param tunnel="yes" name="ChannelID_audio">CHID0000</xsl:with-param>
                <xsl:with-param tunnel="yes" name="ChannelID_text">CHID0001</xsl:with-param>
              </xsl:apply-templates>
            </xsl:when>
            <xsl:otherwise>
              <xsl:apply-templates>
                <xsl:with-param tunnel="yes" name="ChannelID_audio">CH00001</xsl:with-param>
                <xsl:with-param tunnel="yes" name="ChannelID_text">CH00000</xsl:with-param>
              </xsl:apply-templates>
            </xsl:otherwise>
          </xsl:choose>

        </obi:ObiPresentation>
      </xsl:when>
      <xsl:otherwise>
        <Presentation>
          <xsl:attribute name="RootUri">
            <xsl:text>./</xsl:text>
          </xsl:attribute>
          <xsl:call-template name="PresentationFactoriesAndManagers"/>

          <xsl:choose>
            <xsl:when test="./local-name() = 'Presentation'">
              <xsl:apply-templates>
                <xsl:with-param tunnel="yes" name="ChannelID_audio">CHID0000</xsl:with-param>
                <xsl:with-param tunnel="yes" name="ChannelID_text">CHID0001</xsl:with-param>
              </xsl:apply-templates>
            </xsl:when>
            <xsl:otherwise>
              <xsl:apply-templates>
                <xsl:with-param tunnel="yes" name="ChannelID_audio">CH00001</xsl:with-param>
                <xsl:with-param tunnel="yes" name="ChannelID_text">CH00000</xsl:with-param>
              </xsl:apply-templates>
            </xsl:otherwise>
          </xsl:choose>

        </Presentation>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Document root -->
  <xsl:template match="xuk1:mRootNode | xuk2:RootNode">
    <RootNode>
      <xsl:apply-templates/>
    </RootNode>
  </xsl:template>

  <!-- Obi "empty" nodes are ignored (skipped) -->
  <xsl:template match="obi:empty">
    <xsl:choose>
      <xsl:when test="$generateObiFormat">
        <obi:empty>
          <xsl:apply-templates/>
        </obi:empty>
      </xsl:when>
      <xsl:otherwise>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Obi "root" node -->
  <xsl:template match="obi:root">
    <xsl:choose>
      <xsl:when test="$generateObiFormat">
        <obi:root>
          <xsl:apply-templates/>
        </obi:root>
      </xsl:when>
      <xsl:otherwise>
        <TreeNode>
          <xsl:apply-templates/>
        </TreeNode>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Obi "section" nodes -->
  <xsl:template match="obi:section">
    <xsl:choose>
      <xsl:when test="$generateObiFormat">
        <obi:section>
          <xsl:apply-templates/>
        </obi:section>
      </xsl:when>
      <xsl:otherwise>
        <TreeNode>
          <xsl:apply-templates/>
        </TreeNode>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Obi "phrase" nodes -->
  <xsl:template match="obi:phrase">
    <xsl:choose>
      <!-- $generateObiFormat => clone phrase data -->
      <xsl:when test="$generateObiFormat">
        <obi:phrase>
          <xsl:if test="./@kind">
            <xsl:attribute name="kind">
              <xsl:value-of select="./@kind"/>
            </xsl:attribute>
          </xsl:if>
          <xsl:if test="./@page">
            <xsl:attribute name="page">
              <xsl:value-of select="./@page"/>
            </xsl:attribute>
          </xsl:if>
          <xsl:if test="./@pageKind">
            <xsl:attribute name="pageKind">
              <xsl:value-of select="./@pageKind"/>
            </xsl:attribute>
          </xsl:if>
          <xsl:if test="./@pageText">
            <xsl:attribute name="pageText">
              <xsl:value-of select="./@pageText"/>
            </xsl:attribute>
          </xsl:if>
          <xsl:apply-templates/>
        </obi:phrase>
      </xsl:when>
      <!-- $generateTobiFormat => skip heading phrase (do nothing, will be extracted separately to be associated with the level-hd) -->
      <xsl:when test="./@kind = 'Heading'"> </xsl:when>
      <xsl:when
          test="./count(./@kind) = 0
                                and
                            ./count(./preceding-sibling::obi:phrase[@kind = 'Heading' or count(@kind) = 0]) = 0"> </xsl:when>
      <xsl:otherwise>
        <TreeNode>
          <xsl:apply-templates/>
        </TreeNode>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- List of child tree nodes -->
  <xsl:template match="xuk1:mChildren | xuk2:Children">
    <xsl:param tunnel="yes" name="ChannelID_audio">ChannelID_audio</xsl:param>
    <xsl:param tunnel="yes" name="ChannelID_text">ChannelID_text</xsl:param>
    <Children>
      <xsl:choose>
        <xsl:when test="$generateObiFormat">
          <xsl:apply-templates/>
        </xsl:when>
        <!-- $generateTobiFormat => special content injection to meet DTBOOK structural requirements -->
        <!-- Injection of the level's hd heading, using the audio from the first eligable phrase -->
        <xsl:when test="../local-name() = 'section'">
          <TreeNode>
            <Properties>
              <XmlProperty>
                <xsl:attribute name="LocalName">
                  <xsl:text>hd</xsl:text>
                </xsl:attribute>
              </XmlProperty>
              <ChannelsProperty>
                <ChannelMappings>
                  <ChannelMapping>
                    <!--  Channel="CHID0001"  -->
                    <!-- xsl:attribute name="Channel">
                      <xsl:value-of
                            select="$ChannelID_text"
                                                  />
                    </xsl:attribute -->
                    <xsl:attribute name="Channel">
                      <xsl:choose>
                        <xsl:when test="../xuk1:mProperties">
                          <xsl:value-of
                          select="../xuk1:mProperties/xuk1:ChannelsProperty/xuk1:mChannelMappings/xuk1:mChannelMapping/@channel"/>
                        </xsl:when>
                        <xsl:otherwise>
                          <xsl:value-of
                          select="../xuk2:Properties/xuk2:ChannelsProperty/xuk2:ChannelMappings/xuk2:ChannelMapping/@Channel"/>
                        </xsl:otherwise>
                      </xsl:choose>
                    </xsl:attribute>
                    <TextMedia>
                      <Text>
                        <xsl:choose>
                          <xsl:when test="../xuk1:mProperties">
                            <xsl:value-of
                            select="../xuk1:mProperties/xuk1:ChannelsProperty/xuk1:mChannelMappings/xuk1:mChannelMapping/xuk1:TextMedia/xuk1:mText"
                                                  />
                          </xsl:when>
                          <xsl:otherwise>
                            <xsl:value-of
                            select="../xuk2:Properties/xuk2:ChannelsProperty/xuk2:ChannelMappings/xuk2:ChannelMapping/xuk2:TextMedia/xuk2:Text"
                                                  />
                          </xsl:otherwise>
                        </xsl:choose>
                      </Text>
                    </TextMedia>
                  </ChannelMapping>
                  <ChannelMapping>
                    <!--  Channel="CHID0000"  -->
                    <xsl:attribute name="Channel">
                      <xsl:value-of
                            select="$ChannelID_audio"
                                                  />
                    </xsl:attribute>
                    <ManagedAudioMedia>
                      <xsl:choose>
                        <xsl:when test="../xuk1:mChildren">
                          <xsl:attribute name="MediaDataUid">
                            <xsl:value-of
                            select="../xuk1:mChildren/obi:phrase
                                                  [@kind='Heading' or
                                                  count(@kind) = 0
                                                  and
                                                  count(preceding-sibling::obi:phrase[@kind = 'Heading' or count(@kind) = 0]) = 0]
                                                  [1]
                                                  /xuk1:mProperties/xuk1:ChannelsProperty/xuk1:mChannelMappings/xuk1:mChannelMapping/xuk1:ManagedAudioMedia/@audioMediaDataUid"
                                                  />
                          </xsl:attribute>
                        </xsl:when>
                        <xsl:otherwise>
                          <xsl:attribute name="MediaDataUid">
                            <xsl:value-of
                            select="../xuk2:Children/obi:phrase
                                                          [@kind='Heading' or
                                                          count(@kind) = 0
                                                          and
                                                          count(preceding-sibling::obi:phrase[@kind = 'Heading' or count(@kind) = 0]) = 0]
                                                          [1]
                                                          /xuk2:Properties/xuk2:ChannelsProperty/xuk2:ChannelMappings/xuk2:ChannelMapping/xuk2:ManagedAudioMedia/@MediaDataUid"
                                                  />
                          </xsl:attribute>
                        </xsl:otherwise>
                      </xsl:choose>
                    </ManagedAudioMedia>
                  </ChannelMapping>
                </ChannelMappings>
              </ChannelsProperty>
            </Properties>
            <Children/>
          </TreeNode>
          <xsl:choose>
            <xsl:when test="count(./*[local-name() = 'phrase']) = 1">
              <TreeNode>
                <Properties>
                  <XmlProperty>
                    <xsl:attribute name="LocalName">
                      <xsl:text>p</xsl:text>
                    </xsl:attribute>
                  </XmlProperty>
                  <ChannelsProperty>
                    <ChannelMappings>
                      <ChannelMapping>
                        <!--  Channel="CHID0001"  -->
                        <xsl:attribute name="Channel">
                          <xsl:value-of
                                select="$ChannelID_text"
                                                  />
                        </xsl:attribute>
                        <TextMedia>
                          <Text>.</Text>
                        </TextMedia>
                      </ChannelMapping>
                    </ChannelMappings>
                  </ChannelsProperty>
                </Properties>
                <Children/>
              </TreeNode>
            </xsl:when>
            <xsl:otherwise> </xsl:otherwise>
          </xsl:choose>
          <xsl:apply-templates/>
        </xsl:when>
        <!-- this shouldn't create anymore content, as phrases have no child TreeNodes -->
        <xsl:when test="../local-name() = 'phrase'">
          <xsl:apply-templates/>
        </xsl:when>
        <!-- injection of DTBOOK frontmatter and doctitle/docauthor -->
        <xsl:when test="../local-name() = 'root'">
          <TreeNode>
            <Properties>
              <XmlProperty>
                <xsl:attribute name="LocalName">
                  <xsl:text>frontmatter</xsl:text>
                </xsl:attribute>
              </XmlProperty>
            </Properties>
            <Children>
              <TreeNode>
                <Properties>
                  <XmlProperty LocalName="doctitle"/>
                  <ChannelsProperty>
                    <ChannelMappings>
                      <ChannelMapping>
                        <!--  Channel="CHID0001"  -->
                        <xsl:attribute name="Channel">
                          <xsl:value-of
                                select="$ChannelID_text"
                                                  />
                          </xsl:attribute>
                        <TextMedia>
                          <Text>Doc Title</Text>
                        </TextMedia>
                      </ChannelMapping>
                    </ChannelMappings>
                  </ChannelsProperty>
                </Properties>
                <Children/>
              </TreeNode>
              <TreeNode>
                <Properties>
                  <XmlProperty LocalName="docauthor"/>
                  <ChannelsProperty>
                    <ChannelMappings>
                      <ChannelMapping>
                        <!--  Channel="CHID0001"  -->
                        <xsl:attribute name="Channel">
                          <xsl:value-of
                                select="$ChannelID_text"
                                                  />
                        </xsl:attribute>
                        <TextMedia>
                          <Text>Doc Author</Text>
                        </TextMedia>
                      </ChannelMapping>
                    </ChannelMappings>
                  </ChannelsProperty>
                </Properties>
                <Children/>
              </TreeNode>
              <xsl:apply-templates/>
            </Children>
          </TreeNode>
        </xsl:when>
        <xsl:otherwise> </xsl:otherwise>
      </xsl:choose>
    </Children>
  </xsl:template>

  <!-- List of node properties -->
  <xsl:template match="xuk1:mProperties | xuk2:Properties">
    <Properties>
      <xsl:choose>
        <xsl:when test="$generateObiFormat">
          <xsl:apply-templates/>
        </xsl:when>
        <!-- $generateTobiFormat => special content injection to meet DTBOOK structural requirements -->
        <xsl:when test="../local-name() = 'root'">
          <XmlProperty LocalName="book"/>
        </xsl:when>
        <xsl:when test="../local-name() = 'section'">
          <XmlProperty LocalName="level"/>
        </xsl:when>
        <xsl:when test="../local-name() = 'phrase'">
          <xsl:choose>
            <xsl:when test="../@kind = 'Page'">
              <XmlProperty LocalName="pagenum">
                <XmlAttributes>
                  <XmlAttribute LocalName="page">
                    <xsl:attribute name="Value">
                      <xsl:value-of select="../@pageKind"/>
                    </xsl:attribute>
                  </XmlAttribute>
                  <XmlAttribute LocalName="id">
                    <xsl:attribute name="Value">
                      <xsl:text>Page_</xsl:text>
                      <xsl:value-of select="../@page"/>
                    </xsl:attribute>
                  </XmlAttribute>
                </XmlAttributes>
              </XmlProperty>
              <xsl:apply-templates/>
            </xsl:when>
            <xsl:otherwise>
              <XmlProperty LocalName="p"/>
              <xsl:apply-templates/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:otherwise> </xsl:otherwise>
      </xsl:choose>
    </Properties>
  </xsl:template>

  <!-- Root of Audio and Text Channels -->
  <xsl:template match="xuk1:ChannelsProperty | xuk2:ChannelsProperty">
    <xsl:choose>
      <!-- We skip Obi's section text media (which goes in the hd heading instead) -->
      <xsl:when test="$generateTobiFormat and (../../local-name() = 'section')"> </xsl:when>
      <xsl:otherwise>
        <ChannelsProperty>
          <xsl:apply-templates/>
        </ChannelsProperty>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Audio and Text Channels -->
  <xsl:template match="xuk1:mChannelMappings | xuk2:ChannelMappings">
    <xsl:param tunnel="yes" name="ChannelID_audio">ChannelID_audio</xsl:param>
    <xsl:param tunnel="yes" name="ChannelID_text">ChannelID_text</xsl:param>
    <ChannelMappings>
      <xsl:choose>
        <xsl:when test="$generateTobiFormat and (../../../local-name() = 'phrase')">
          <ChannelMapping>
            <!--  Channel="CHID0001"  -->
            <xsl:attribute name="Channel">
              <xsl:value-of
                    select="$ChannelID_text"
                                                  />
            </xsl:attribute>
            <TextMedia>
              <xsl:choose>
                <xsl:when test="../../../@kind = 'Page'">
                  <Text>
                    <xsl:value-of select="../../../@pageText"/>
                  </Text>
                </xsl:when>
                <xsl:otherwise>
                  <Text>
                    Phrase <xsl:text> </xsl:text>
                    <xsl:choose>
                      <xsl:when test="xuk1:mChannelMapping">
                        <xsl:value-of
                            select="xuk1:mChannelMapping/xuk1:ManagedAudioMedia/@audioMediaDataUid"
                                                />
                      </xsl:when>
                      <xsl:otherwise>
                        <xsl:value-of
                            select="xuk2:ChannelMapping/xuk2:ManagedAudioMedia/@MediaDataUid"
                                                />
                      </xsl:otherwise>
                    </xsl:choose>
                  </Text>
                </xsl:otherwise>
              </xsl:choose>
            </TextMedia>
          </ChannelMapping>
        </xsl:when>
        <xsl:otherwise> </xsl:otherwise>
      </xsl:choose>
      <xsl:apply-templates/>
    </ChannelMappings>
  </xsl:template>

  <!-- Audio and Text Channels (clone from source) -->
  <xsl:template match="xuk1:mChannelMapping | xuk2:ChannelMapping">
    <ChannelMapping>
      <xsl:attribute name="Channel">
        <xsl:choose>
          <xsl:when test="@channel">
            <xsl:value-of select="@channel"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="@Channel"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:apply-templates/>
    </ChannelMapping>
  </xsl:template>

  <!-- Text media -->
  <xsl:template match="xuk1:TextMedia | xuk2:TextMedia">
    <TextMedia>
      <xsl:apply-templates/>
    </TextMedia>
  </xsl:template>

  <!-- Text content -->
  <xsl:template match="xuk1:mText | xuk2:Text">
    <Text>
      <xsl:value-of select="text()"/>
    </Text>
  </xsl:template>

  <!-- Audio media -->
  <xsl:template match="xuk1:ManagedAudioMedia | xuk2:ManagedAudioMedia">
    <ManagedAudioMedia>
      <xsl:attribute name="MediaDataUid">
        <xsl:choose>
          <xsl:when test="@audioMediaDataUid">
            <xsl:value-of select="@audioMediaDataUid"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="@MediaDataUid"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:apply-templates/>
    </ManagedAudioMedia>
  </xsl:template>

  <!-- XML element -->
  <xsl:template match="xuk1:XmlProperty | xuk2:XmlProperty">
    <XmlProperty>
      <xsl:choose>
        <xsl:when test="@localName">
          <xsl:attribute name="LocalName">
            <xsl:value-of select="@localName"/>
          </xsl:attribute>
          <xsl:if test="@namespaceUri">
            <xsl:attribute name="NamespaceUri">
              <xsl:value-of select="@namespaceUri"/>
            </xsl:attribute>
          </xsl:if>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="LocalName">
            <xsl:value-of select="@LocalName"/>
          </xsl:attribute>
          <xsl:if test="@NamespaceUri">
            <xsl:attribute name="NamespaceUri">
              <xsl:value-of select="@NamespaceUri"/>
            </xsl:attribute>
          </xsl:if>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:apply-templates/>
    </XmlProperty>
  </xsl:template>

  <!-- List of XML attributes -->
  <xsl:template match="xuk1:mXmlAttributes | xuk2:XmlAttributes">
    <XmlAttributes>
      <xsl:apply-templates/>
    </XmlAttributes>
  </xsl:template>

  <!-- XML attribute -->
  <xsl:template match="xuk1:XmlAttribute | xuk2:XmlAttribute">
    <XmlAttribute>
      <xsl:choose>
        <xsl:when test="@localName">
          <xsl:attribute name="LocalName">
            <xsl:value-of select="@localName"/>
          </xsl:attribute>
          <xsl:attribute name="Value">
            <xsl:value-of select="@Value"/>
          </xsl:attribute>
          <xsl:if test="@namespaceUri">
            <xsl:attribute name="NamespaceUri">
              <xsl:value-of select="@namespaceUri"/>
            </xsl:attribute>
          </xsl:if>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="LocalName">
            <xsl:value-of select="@LocalName"/>
          </xsl:attribute>
          <xsl:attribute name="Value">
            <xsl:value-of select="@Value"/>
          </xsl:attribute>
          <xsl:if test="@NamespaceUri">
            <xsl:attribute name="NamespaceUri">
              <xsl:value-of select="@NamespaceUri"/>
            </xsl:attribute>
          </xsl:if>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:apply-templates/>
    </XmlAttribute>
  </xsl:template>

  <!-- List of MediaDatas -->
  <xsl:template match="xuk1:mMediaData | xuk2:MediaDatas">
    <MediaDatas>
      <xsl:apply-templates/>
    </MediaDatas>
  </xsl:template>

  <!-- MediaData container -->
  <xsl:template match="xuk1:mMediaDataItem | xuk2:MediaDataItem">
    <xsl:apply-templates/>
  </xsl:template>

  <!-- XML attribute -->
  <xsl:template match="xuk1:WavAudioMediaData | xuk2:WavAudioMediaData">
    <WavAudioMediaData>
      <xsl:choose>
        <xsl:when test="../@uid">
          <xsl:attribute name="Uid">
            <xsl:value-of select="../@uid"/>
          </xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="Uid">
            <xsl:value-of select="@Uid"/>
          </xsl:attribute>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:apply-templates/>
    </WavAudioMediaData>
  </xsl:template>

  <!-- PCM format (skipped, as we re-create everything "manually") -->
  <xsl:template match="xuk1:mPCMFormat | xuk2:PCMFormat"/>

  <!-- List of WavClips -->
  <xsl:template match="xuk1:mWavClips | xuk2:WavClips">
    <WavClips>
      <xsl:apply-templates/>
    </WavClips>
  </xsl:template>

  <!-- WavClip -->
  <xsl:template match="xuk1:WavClip | xuk2:WavClip">
    <WavClip>
      <xsl:choose>
        <xsl:when test="@dataProvider">
          <xsl:attribute name="DataProvider">
            <xsl:value-of select="@dataProvider"/>
          </xsl:attribute>
          <xsl:if test="@clipBegin">
            <xsl:attribute name="ClipBegin">
              <xsl:value-of select="@clipBegin"/>
            </xsl:attribute>
          </xsl:if>
          <xsl:choose>
            <xsl:when test="@clipEnd">
              <xsl:attribute name="ClipEnd">
                <xsl:value-of select="@clipEnd"/>
              </xsl:attribute>
            </xsl:when>
            <xsl:otherwise> </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="DataProvider">
            <xsl:value-of select="@DataProvider"/>
          </xsl:attribute>
          <xsl:if test="@ClipBegin">
            <xsl:attribute name="ClipBegin">
              <xsl:value-of select="@ClipBegin"/>
            </xsl:attribute>
          </xsl:if>
          <xsl:choose>
            <xsl:when test="@ClipEnd">
              <xsl:attribute name="ClipEnd">
                <xsl:value-of select="@ClipEnd"/>
              </xsl:attribute>
            </xsl:when>
            <xsl:otherwise> </xsl:otherwise>
          </xsl:choose>
        </xsl:otherwise>
      </xsl:choose>
    </WavClip>
  </xsl:template>

  <!-- DataProviderManager -->
  <xsl:template match="xuk1:mDataProviderManager">
    <xsl:apply-templates/>
  </xsl:template>

  <!-- FileDataProviderManager -->
  <xsl:template match="xuk1:FileDataProviderManager | xuk2:DataProviderManager">
    <DataProviderManager>
      <xsl:choose>
        <xsl:when test="@dataFileDirectoryPath">
          <xsl:attribute name="DataFileDirectoryPath">
            <xsl:value-of select="@dataFileDirectoryPath"/>
          </xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="DataFileDirectoryPath">
            <xsl:value-of select="@DataFileDirectoryPath"/>
          </xsl:attribute>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:apply-templates/>
    </DataProviderManager>
  </xsl:template>

  <!-- List of DataProviders -->
  <xsl:template match="xuk1:mDataProviders | xuk2:DataProviders">
    <DataProviders>
      <xsl:apply-templates/>
    </DataProviders>
  </xsl:template>

  <!-- DataProvider container -->
  <xsl:template match="xuk1:mDataProviderItem | xuk2:DataProviderItem">
    <xsl:apply-templates/>
  </xsl:template>

  <!-- FileDataProvider -->
  <xsl:template match="xuk1:FileDataProvider | xuk2:FileDataProvider">
    <FileDataProvider>
      <xsl:choose>
        <xsl:when test="../@uid">
          <xsl:attribute name="Uid">
            <xsl:value-of select="../@uid"/>
          </xsl:attribute>
          <xsl:attribute name="DataFileRelativePath">
            <xsl:value-of select="@dataFileRelativePath"/>
          </xsl:attribute>
          <xsl:attribute name="MimeType">
            <xsl:value-of select="@mimeType"/>
          </xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="Uid">
            <xsl:value-of select="@Uid"/>
          </xsl:attribute>
          <xsl:attribute name="DataFileRelativePath">
            <xsl:value-of select="@DataFileRelativePath"/>
          </xsl:attribute>
          <xsl:attribute name="MimeType">
            <xsl:value-of select="@MimeType"/>
          </xsl:attribute>
        </xsl:otherwise>
      </xsl:choose>
    </FileDataProvider>
  </xsl:template>

  <!-- MediaDataManager container -->
  <xsl:template match="xuk1:mMediaDataManager">
    <xsl:apply-templates/>
  </xsl:template>

  <!-- MediaDataManager -->
  <xsl:template match="obi:DataManager | xuk2:MediaDataManager">
    <MediaDataManager enforceSinglePCMFormat="true">
      <xsl:apply-templates/>
    </MediaDataManager>
  </xsl:template>

  <!-- Default PCM format -->
  <xsl:template match="xuk1:mDefaultPCMFormat | xuk2:DefaultPCMFormat">
    <DefaultPCMFormat>
      <xsl:apply-templates/>
    </DefaultPCMFormat>
  </xsl:template>

  <!-- PCM format info -->
  <xsl:template match="xuk1:PCMFormatInfo | xuk2:PCMFormatInfo">
    <PCMFormatInfo>
      <xsl:choose>
        <xsl:when test="@numberOfChannels">
          <xsl:attribute name="NumberOfChannels">
            <xsl:value-of select="@numberOfChannels"/>
          </xsl:attribute>
          <xsl:attribute name="SampleRate">
            <xsl:value-of select="@sampleRate"/>
          </xsl:attribute>
          <xsl:attribute name="BitDepth">
            <xsl:value-of select="@bitDepth"/>
          </xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="NumberOfChannels">
            <xsl:value-of select="@NumberOfChannels"/>
          </xsl:attribute>
          <xsl:attribute name="SampleRate">
            <xsl:value-of select="@SampleRate"/>
          </xsl:attribute>
          <xsl:attribute name="BitDepth">
            <xsl:value-of select="@BitDepth"/>
          </xsl:attribute>
        </xsl:otherwise>
      </xsl:choose>
    </PCMFormatInfo>
  </xsl:template>

  <!-- List of metadatas -->
  <xsl:template match="xuk1:mMetadata | xuk2:Metadatas">
    <Metadatas>
      <xsl:apply-templates/>
    </Metadatas>
  </xsl:template>

  <!-- Metadata -->
  <xsl:template match="xuk1:Metadata">
    <Metadata>
      <MetadataAttribute>
        <xsl:attribute name="Name">
          <xsl:value-of select="@name"/>
        </xsl:attribute>
        <xsl:attribute name="Value">
          <xsl:value-of select="@content"/>
        </xsl:attribute>
      </MetadataAttribute>
    </Metadata>
  </xsl:template>

  <!-- Metadata -->
  <xsl:template match="xuk2:Metadata">
    <Metadata>
      <xsl:apply-templates/>
    </Metadata>
  </xsl:template>

  <!-- Metadata attribute -->
  <xsl:template match="xuk2:MetadataAttribute">
    <MetadataAttribute>
      <xsl:attribute name="Name">
        <xsl:value-of select="@Name"/>
      </xsl:attribute>
      <xsl:attribute name="Value">
        <xsl:value-of select="@Value"/>
      </xsl:attribute>
    </MetadataAttribute>
  </xsl:template>
</xsl:stylesheet>
