<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.daisy.org/urakawa/xuk/2.0"
    xmlns:oldXuk="http://www.daisy.org/urakawa/xuk/1.0"
    xmlns:newXuk="http://www.daisy.org/urakawa/xuk/2.0" xmlns:obi="http://www.daisy.org/urakawa/obi"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    exclude-result-prefixes="xs" version="2.0">
    <xsl:output method="xml" indent="yes" omit-xml-declaration="no" version="1.0"/>
    <!-- xsl:strip-space elements="*"/ -->
    <!-- xsl:template match="text()[not(normalize-space())]"/ -->
    <xsl:template match="text()"/>

    <xsl:variable name="skipExtraTobiDTBookData" as="xs:boolean" select="false()"/>

    <xsl:template match="/">
        <xsl:apply-templates/>
    </xsl:template>
    <xsl:template match="oldXuk:Xuk | newXuk:Xuk">
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
    <xsl:template match="oldXuk:Project | newXuk:Project">
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
                        AssemblyName="Obi" AssemblyVersion="0.0.0.0"
                        FullName="Obi.ObiPresentation"/>
                </RegisteredTypes>
            </PresentationFactory>
            <xsl:apply-templates/>
        </Project>
    </xsl:template>
    <xsl:template match="oldXuk:mPresentations | newXuk:Presentations">
        <Presentations>
            <xsl:apply-templates/>
        </Presentations>
    </xsl:template>
    <xsl:template match="obi:Presentation | obi:ObiPresentation">
        <obi:ObiPresentation>
            <xsl:attribute name="RootUri">
                <xsl:text>./</xsl:text>
            </xsl:attribute>
            <TreeNodeFactory>
                <RegisteredTypes>
                    <Type XukLocalName="TreeNode"
                        XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
                        AssemblyName="UrakawaSDK.core" AssemblyVersion="2.0.0.0"
                        FullName="urakawa.core.TreeNode"/>
                    <Type XukLocalName="root" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
                        BaseXukLocalName="TreeNode"
                        BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
                        AssemblyName="Obi" AssemblyVersion="0.0.0.0" FullName="Obi.ObiRootNode"/>
                    <Type XukLocalName="section" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
                        BaseXukLocalName="TreeNode"
                        BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
                        AssemblyName="Obi" AssemblyVersion="0.0.0.0" FullName="Obi.SectionNode"/>
                    <Type XukLocalName="phrase" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
                        BaseXukLocalName="TreeNode"
                        BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
                        AssemblyName="Obi" AssemblyVersion="0.0.0.0" FullName="Obi.PhraseNode"/>
                    <Type XukLocalName="empty" XukNamespaceUri="http://www.daisy.org/urakawa/obi"
                        BaseXukLocalName="TreeNode"
                        BaseXukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
                        AssemblyName="Obi" AssemblyVersion="0.0.0.0" FullName="Obi.EmptyNode"/>
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
                    <Type XukLocalName="Channel"
                        XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
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
                    <Type XukLocalName="Metadata"
                        XukNamespaceUri="http://www.daisy.org/urakawa/xuk/2.0"
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
            <ChannelsManager>
                <Channels>
                    <AudioChannel Uid="CHID0000" Name="The Audio Channel"/>
                    <TextChannel Uid="CHID0001" Name="The Text Channel"/>
                    <ImageChannel Uid="CHID0002" Name="The Image Channel"/>
                </Channels>
            </ChannelsManager>
            <ExternalFileDataManager>
                <ExternalFileDatas/>
            </ExternalFileDataManager>
            <xsl:apply-templates/>
        </obi:ObiPresentation>
    </xsl:template>
    <xsl:template match="oldXuk:mRootNode | newXuk:RootNode">
        <RootNode>
            <xsl:apply-templates/>
        </RootNode>
    </xsl:template>
    <xsl:template match="obi:empty">
        <!-- obi:empty>
            <xsl:apply-templates/>
            </obi:empty -->
    </xsl:template>
    <xsl:template match="obi:root">
        <obi:root>
            <xsl:apply-templates/>
        </obi:root>
    </xsl:template>
    <xsl:template match="obi:section">
        <obi:section>
            <xsl:apply-templates/>
        </obi:section>
    </xsl:template>
    <xsl:template match="obi:phrase">
        <xsl:choose>
            <xsl:when test="$skipExtraTobiDTBookData">
                <obi:phrase>
                    <xsl:apply-templates/>
                </obi:phrase>
            </xsl:when>
            <xsl:when
                test="(./@kind = 'Heading') or (./Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='kind' and @Value='Heading'])"> </xsl:when>
            <xsl:when test="./count(./preceding-sibling::*) = 0"> </xsl:when>
            <xsl:when test="./count(./preceding-sibling::*[local-name()!='empty']) = 0"> </xsl:when>
            <xsl:otherwise>
                <obi:phrase>
                    <xsl:apply-templates/>
                </obi:phrase>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <xsl:template match="oldXuk:mChildren | newXuk:Children">
        <Children>
            <xsl:choose>
                <xsl:when test="$skipExtraTobiDTBookData">
                    <xsl:apply-templates/>
                </xsl:when>
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
                                    <ChannelMapping Channel="CHID0001">
                                        <TextMedia>
                                            <Text>
                                                <xsl:choose>
                                                    <xsl:when test="../oldXuk:mProperties">
                                                        <xsl:value-of
                                                            select="../oldXuk:mProperties/oldXuk:ChannelsProperty/oldXuk:mChannelMappings/oldXuk:mChannelMapping/oldXuk:TextMedia/oldXuk:mText"
                                                        />
                                                    </xsl:when>
                                                    <xsl:otherwise>
                                                        <xsl:value-of
                                                            select="../newXuk:Properties/newXuk:ChannelsProperty/newXuk:ChannelMappings/newXuk:ChannelMapping/newXuk:TextMedia/newXuk:Text"
                                                        />
                                                    </xsl:otherwise>
                                                </xsl:choose>
                                            </Text>
                                        </TextMedia>
                                    </ChannelMapping>
                                    <ChannelMapping Channel="CHID0000">
                                        <ManagedAudioMedia>
                                            <xsl:choose>
                                                <xsl:when test="../oldXuk:mChildren">
                                                    <xsl:attribute name="MediaDataUid">
                                                        <xsl:value-of
                                                            select="../oldXuk:mChildren/obi:phrase[@kind='Heading' or count(./preceding-sibling::*)=0 or count(./preceding-sibling::*[local-name()!='empty'])=0]/oldXuk:mProperties/oldXuk:ChannelsProperty/oldXuk:mChannelMappings/oldXuk:mChannelMapping/oldXuk:ManagedAudioMedia/@audioMediaDataUid"
                                                        />
                                                    </xsl:attribute>
                                                </xsl:when>
                                                <xsl:otherwise>
                                                    <xsl:attribute name="MediaDataUid">
                                                        <xsl:value-of
                                                            select="../newXuk:Children/
                                                            obi:phrase[
                                                            ./Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='kind']/@Value='Heading'
                                                            or count(./preceding-sibling::*)=0
                                                            or count(./preceding-sibling::*[local-name()!='empty'])=0]
                                                            /newXuk:Properties/newXuk:ChannelsProperty/newXuk:ChannelMappings/newXuk:ChannelMapping/newXuk:ManagedAudioMedia/@MediaDataUid"
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
                        <xsl:when test="count(./*[local-name()!='empty']) = 1">
                            <TreeNode>
                                <Properties>
                                    <XmlProperty>
                                        <xsl:attribute name="LocalName">
                                            <xsl:text>p</xsl:text>
                                        </xsl:attribute>
                                    </XmlProperty>
                                    <ChannelsProperty>
                                        <ChannelMappings>
                                            <ChannelMapping Channel="CHID0001">
                                                <TextMedia>
                                                    <Text> </Text>
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
                <xsl:when test="../local-name() = 'phrase'">
                    <xsl:apply-templates/>
                </xsl:when>
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
                                            <ChannelMapping Channel="CHID0001">
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
                                            <ChannelMapping Channel="CHID0001">
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
    <xsl:template match="oldXuk:mProperties | newXuk:Properties">
        <Properties>
            <xsl:choose>
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
                        </xsl:when>
                        <xsl:when
                            test="../Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='kind' and @Value='Page']">
                            <XmlProperty LocalName="pagenum">
                                <XmlAttributes>
                                    <XmlAttribute LocalName="page">
                                        <xsl:attribute name="Value">
                                            <xsl:value-of
                                                select="../Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='pageKind']/@Value"
                                            />
                                        </xsl:attribute>
                                    </XmlAttribute>
                                    <XmlAttribute LocalName="id">
                                        <xsl:attribute name="Value">
                                            <xsl:text>Page_</xsl:text>
                                            <xsl:value-of
                                                select="../Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='page']/@Value"
                                            />
                                        </xsl:attribute>
                                    </XmlAttribute>
                                </XmlAttributes>
                            </XmlProperty>
                        </xsl:when>
                        <xsl:otherwise>
                            <XmlProperty LocalName="p"/>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:when>
                <xsl:otherwise> </xsl:otherwise>
            </xsl:choose>
            <xsl:apply-templates/>
        </Properties>
    </xsl:template><xsl:template match="oldXuk:ChannelsProperty | newXuk:ChannelsProperty">
        <xsl:choose>
            <xsl:when test="not($skipExtraTobiDTBookData) and (../../local-name() = 'section')"> </xsl:when>
            <xsl:otherwise>
                <ChannelsProperty>
                    <xsl:apply-templates/>
                </ChannelsProperty>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <xsl:template match="oldXuk:mChannelMappings | newXuk:ChannelMappings">
        <ChannelMappings>
            <xsl:choose>
                <xsl:when test="not($skipExtraTobiDTBookData) and (../../../local-name() = 'phrase')">
                    <ChannelMapping Channel="CHID0001">
                        <TextMedia>
                            <xsl:choose>
                                <xsl:when test="../../../@kind">
                                    <xsl:choose>
                                        <xsl:when test="../../../@kind = 'Page'">
                                            <Text>
                                                <xsl:value-of select="../../../@pageText"/>
                                            </Text>
                                        </xsl:when>
                                        <xsl:otherwise>
                                            <Text>Phrase <xsl:text> </xsl:text>
                                                <xsl:value-of
                                                  select="oldXuk:mChannelMapping/oldXuk:ManagedAudioMedia/@audioMediaDataUid"
                                                />
                                            </Text>
                                        </xsl:otherwise>
                                    </xsl:choose>
                                </xsl:when>
                                <xsl:otherwise>
                                    <xsl:choose>
                                        <xsl:when
                                            test="../../../Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='kind']/@Value='Page'">
                                            <Text>
                                                <xsl:value-of
                                                  select="../../../Properties/XmlProperty/XmlAttributes/XmlAttribute[@LocalName='pageText']/@Value"
                                                />
                                            </Text>
                                        </xsl:when>
                                        <xsl:otherwise>
                                            <Text>Phrase <xsl:text> </xsl:text>
                                                <xsl:value-of
                                                  select="newXuk:ChannelMapping/newXuk:ManagedAudioMedia/@MediaDataUid"
                                                />
                                            </Text>
                                        </xsl:otherwise>
                                    </xsl:choose>
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
    <xsl:template match="oldXuk:mChannelMapping">
        <ChannelMapping>
            <xsl:attribute name="Channel">
                <xsl:value-of select="@channel"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </ChannelMapping>
    </xsl:template>
    <xsl:template match="newXuk:ChannelMapping">
        <ChannelMapping>
            <xsl:attribute name="Channel">
                <xsl:value-of select="@Channel"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </ChannelMapping>
    </xsl:template>
    <xsl:template match="oldXuk:TextMedia | newXuk:TextMedia">
        <TextMedia>
            <xsl:apply-templates/>
        </TextMedia>
    </xsl:template>
    <xsl:template match="oldXuk:mText | newXuk:Text">
        <Text>
            <xsl:value-of select="text()"/>
        </Text>
    </xsl:template>
    <xsl:template match="oldXuk:ManagedAudioMedia">
        <ManagedAudioMedia>
            <xsl:attribute name="MediaDataUid">
                <xsl:value-of select="@audioMediaDataUid"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </ManagedAudioMedia>
    </xsl:template>
    <xsl:template match="newXuk:ManagedAudioMedia">
        <ManagedAudioMedia>
            <xsl:attribute name="MediaDataUid">
                <xsl:value-of select="@MediaDataUid"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </ManagedAudioMedia>
    </xsl:template>
    <xsl:template match="oldXuk:XmlProperty">
        <XmlProperty>
            <xsl:attribute name="LocalName">
                <xsl:value-of select="@localName"/>
            </xsl:attribute>
            <xsl:attribute name="NamespaceUri">
                <xsl:value-of select="@namespaceUri"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </XmlProperty>
    </xsl:template>
    <xsl:template match="newXuk:XmlProperty">
        <XmlProperty>
            <xsl:attribute name="LocalName">
                <xsl:value-of select="@LocalName"/>
            </xsl:attribute>
            <xsl:attribute name="NamespaceUri">
                <xsl:value-of select="@NamespaceUri"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </XmlProperty>
    </xsl:template>
    <xsl:template match="oldXuk:mXmlAttributes | newXuk:XmlAttributes">
        <XmlAttributes>
            <xsl:apply-templates/>
        </XmlAttributes>
    </xsl:template>
    <xsl:template match="oldXuk:XmlAttribute">
        <XmlAttribute>
            <xsl:attribute name="LocalName">
                <xsl:value-of select="@localName"/>
            </xsl:attribute>
            <xsl:attribute name="Value">
                <xsl:value-of select="@Value"/>
            </xsl:attribute>
            <xsl:attribute name="NamespaceUri">
                <xsl:value-of select="@namespaceUri"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </XmlAttribute>
    </xsl:template>
    <xsl:template match="newXuk:XmlAttribute">
        <XmlAttribute>
            <xsl:attribute name="LocalName">
                <xsl:value-of select="@LocalName"/>
            </xsl:attribute>
            <xsl:attribute name="Value">
                <xsl:value-of select="@Value"/>
            </xsl:attribute>
            <xsl:attribute name="NamespaceUri">
                <xsl:value-of select="@NamespaceUri"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </XmlAttribute>
    </xsl:template>
    <xsl:template match="oldXuk:mMediaData | newXuk:MediaDatas">
        <MediaDatas>
            <xsl:apply-templates/>
        </MediaDatas>
    </xsl:template>
    <xsl:template match="oldXuk:mMediaDataItem | newXuk:MediaDataItem">
        <xsl:apply-templates/>
    </xsl:template>
    <xsl:template match="oldXuk:WavAudioMediaData">
        <WavAudioMediaData>
            <xsl:attribute name="Uid">
                <xsl:value-of select="../@uid"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </WavAudioMediaData>
    </xsl:template>
    <xsl:template match="newXuk:WavAudioMediaData">
        <WavAudioMediaData>
            <xsl:attribute name="Uid">
                <xsl:value-of select="../@Uid"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </WavAudioMediaData>
    </xsl:template>
    <xsl:template match="oldXuk:mPCMFormat | newXuk:PCMFormat">
        <!-- PCMFormat>
            <xsl:apply-templates/>
        </PCMFormat -->
    </xsl:template>
    <xsl:template match="oldXuk:mWavClips | newXuk:WavClips">
        <WavClips>
            <xsl:apply-templates/>
        </WavClips>
    </xsl:template>
    <xsl:template match="oldXuk:WavClip">
        <WavClip>
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
        </WavClip>
    </xsl:template>
    <xsl:template match="newXuk:WavClip">
        <WavClip>
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
        </WavClip>
    </xsl:template>
    <xsl:template match="oldXuk:mDataProviderManager | newXuk:DataProviderManager">
        <xsl:apply-templates/>
    </xsl:template>
    <xsl:template match="oldXuk:FileDataProviderManager">
        <DataProviderManager>
            <xsl:attribute name="DataFileDirectoryPath">
                <xsl:value-of select="@dataFileDirectoryPath"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </DataProviderManager>
    </xsl:template>
    <xsl:template match="newXuk:FileDataProviderManager">
        <DataProviderManager>
            <xsl:attribute name="DataFileDirectoryPath">
                <xsl:value-of select="@DataFileDirectoryPath"/>
            </xsl:attribute>
            <xsl:apply-templates/>
        </DataProviderManager>
    </xsl:template>
    <xsl:template match="oldXuk:mDataProviders | newXuk:DataProviders">
        <DataProviders>
            <xsl:apply-templates/>
        </DataProviders>
    </xsl:template>
    <xsl:template match="oldXuk:mDataProviderItem | newXuk:DataProviderItem">
        <xsl:apply-templates/>
    </xsl:template>
    <xsl:template match="oldXuk:FileDataProvider">
        <FileDataProvider>
            <xsl:attribute name="Uid">
                <xsl:value-of select="../@uid"/>
            </xsl:attribute>
            <xsl:attribute name="DataFileRelativePath">
                <xsl:value-of select="@dataFileRelativePath"/>
            </xsl:attribute>
            <xsl:attribute name="MimeType">
                <xsl:value-of select="@mimeType"/>
            </xsl:attribute>
        </FileDataProvider>
    </xsl:template>
    <xsl:template match="newXuk:FileDataProvider">
        <FileDataProvider>
            <xsl:attribute name="Uid">
                <xsl:value-of select="../@Uid"/>
            </xsl:attribute>
            <xsl:attribute name="DataFileRelativePath">
                <xsl:value-of select="@DataFileRelativePath"/>
            </xsl:attribute>
            <xsl:attribute name="MimeType">
                <xsl:value-of select="@MimeType"/>
            </xsl:attribute>
        </FileDataProvider>
    </xsl:template>
    <xsl:template match="oldXuk:mMediaDataManager | newXuk:MediaDataManager">
        <xsl:apply-templates/>
    </xsl:template>
    <xsl:template match="obi:DataManager | newXuk:MediaDataManager">
        <MediaDataManager enforceSinglePCMFormat="true">
            <xsl:apply-templates/>
        </MediaDataManager>
    </xsl:template>
    <xsl:template match="oldXuk:mDefaultPCMFormat | newXuk:DefaultPCMFormat">
        <DefaultPCMFormat>
            <xsl:apply-templates/>
        </DefaultPCMFormat>
    </xsl:template>
    <xsl:template match="oldXuk:PCMFormatInfo">
        <PCMFormatInfo>
            <xsl:attribute name="NumberOfChannels">
                <xsl:value-of select="@numberOfChannels"/>
            </xsl:attribute>
            <xsl:attribute name="SampleRate">
                <xsl:value-of select="@sampleRate"/>
            </xsl:attribute>
            <xsl:attribute name="BitDepth">
                <xsl:value-of select="@bitDepth"/>
            </xsl:attribute>
        </PCMFormatInfo>
    </xsl:template>
    <xsl:template match="newXuk:PCMFormatInfo">
        <PCMFormatInfo>
            <xsl:attribute name="NumberOfChannels">
                <xsl:value-of select="@NumberOfChannels"/>
            </xsl:attribute>
            <xsl:attribute name="SampleRate">
                <xsl:value-of select="@SampleRate"/>
            </xsl:attribute>
            <xsl:attribute name="BitDepth">
                <xsl:value-of select="@BitDepth"/>
            </xsl:attribute>
        </PCMFormatInfo>
    </xsl:template>
    <xsl:template match="oldXuk:mMetadata | newXuk:Metadatas">
        <Metadatas>
            <xsl:apply-templates/>
        </Metadatas>
    </xsl:template>
    <xsl:template match="oldXuk:Metadata">
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
    <xsl:template match="newXuk:Metadata">
        <Metadata>
            <xsl:apply-templates/>
        </Metadata>
    </xsl:template>
    <xsl:template match="newXuk:MetadataAttribute">
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
