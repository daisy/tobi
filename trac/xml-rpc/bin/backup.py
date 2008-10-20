#!/usr/bin/env python
# encoding: utf-8
# Author: Romain Deltour - 2008
# 
# Required lib:
# http://codespeak.net/lxml/installation.html
# http://lsimons.wordpress.com/2008/08/31/how-to-install-lxml-python-module-on-mac-os-105-leopard/

import sys
import os
import re
import xmlrpclib
import socket
import urllib
import getpass
from optparse import OptionParser
from lxml import etree
from glob import glob

FILENAME_SLASH_REPLACE = '**'
FILENAME_SPACE_REPLACE = '%20'

socket.setdefaulttimeout(30)

outdir_src = os.path.normpath(os.path.join(os.path.dirname(__file__),'..','source'))
outdir_html = os.path.normpath(os.path.join(os.path.dirname(__file__),'..','html'))
outdir_att = outdir_html # os.path.normpath(os.path.join(os.path.dirname(__file__),'..','attachments'))

def loaddefaulttracpages():
	xml_path = os.path.join(os.path.dirname(__file__),'defaulttracpages.xml')
	xml = etree.parse(xml_path)
	return set(xml.xpath('//tracwikiword/@name'))

	
def loadcredentials():
	xml_path = os.path.join(os.path.dirname(__file__),'config.xml')
	xml = etree.parse(xml_path)
	user = xml.xpath('string(//user)')
	password = xml.xpath('string(//password)')
	server = xml.xpath('string(//server)')
	module = xml.xpath('string(//module)')
	return (user, password, server, module)
	
def initserver(server,user,password):
	global verbose, xmlrpc
	if verbose: print "Initializing server connection..."
	match = re.match('(\w+)://(?:[^@\s]+@)?([^@\s]+)$',server)
	if not match:
		sys.exit("Error: Invalid server URL '%s'" % server)
	url = match.group(1)+'://'+user+':'+urllib.quote(password)+'@'+match.group(2)+'/login/xmlrpc'
	xmlrpc = xmlrpclib.ServerProxy(url)
	try:
		xmlrpc.wiki.getRPCVersionSupported()
	except Exception, inst:
		sys.exit("Error: couldn't connect to server '%s' - %s" % (url, inst))
	

def forPages(pages,func,outdir,excludelist):
	global FILENAME_SLASH_REPLACE, FILENAME_SPACE_REPLACE
	global xmlrpc
	for page in pages: 
		if not page in excludelist:
			func(page, page.replace('/',FILENAME_SLASH_REPLACE).replace(' ',FILENAME_SPACE_REPLACE),outdir)

def downloadSource(page,pagefile,outdir):
	global verbose, xmlrpc
	if verbose: print "- downloading wiki source: " + page
	fpath = os.path.join(outdir,pagefile+".txt")
	if not os.path.isdir(os.path.dirname(fpath)):
		os.makedirs(os.path.dirname(fpath))
	f=open(fpath, 'w')
	try:
		source = xmlrpc.wiki.getPage(page)
		f.write(source.encode('utf-8'))
	except Exception, inst:
		print "Error: %s" % inst
	finally:
		f.close


def downloadHtml(page,pagefile,outdir):
	global verbose, xmlrpc
	if verbose: print "- downloading html page: " + page
	fpath = os.path.join(outdir,pagefile+".original.html")
	if not os.path.isdir(os.path.dirname(fpath)):
		os.makedirs(os.path.dirname(fpath))
	f=open(fpath, 'w')
	try:
		html = xmlrpc.wiki.getPageHTML(page)
		f.write(html.encode('utf-8'))
	except Exception, inst:
		print "Error: %s" % inst
	finally:
		f.close


def downloadAttachments(page,pagefile,outdir):
	global verbose, xmlrpc
	attachments = xmlrpc.wiki.listAttachments(page)
	for att in attachments:
		if verbose: print "- downloading attachment: " + att
		fpath = os.path.join(outdir,att.replace('/',FILENAME_SLASH_REPLACE).replace(' ',FILENAME_SPACE_REPLACE))
		if not os.path.isdir(os.path.dirname(fpath)):
			os.makedirs(os.path.dirname(fpath))
		f=open(fpath, 'wb')
		try:
			o = xmlrpc.wiki.getAttachment(att)
			f.write(o.data)
		except Exception, inst:
			print "Error: %s" % inst
		finally:
			f.close

def cleanhtml(pages,dir_html,server,modulename,excludelist):
	global FILENAME_SLASH_REPLACE, FILENAME_SPACE_REPLACE
	global verbose
	xslt_file = os.path.join(os.path.dirname(__file__), "cleanhtml.xsl")
	xslt_doc = etree.parse(xslt_file)
	xslt = etree.XSLT(xslt_doc)
	for page in pages:
		if page in excludelist: continue
		page=page.replace('/',FILENAME_SLASH_REPLACE).replace(' ',FILENAME_SPACE_REPLACE)
		html = os.path.join(dir_html,page+".original.html")
		htmldest = os.path.join(dir_html,page+".html")
		if verbose: print '- cleaning html file: ' + html
		test = "\"test\""
		try:
			xml_doc = etree.parse(html)
			result = xslt(xml_doc,
						server="\""+server+"\"",
						modulename="\""+modulename+"\"",
						pagename="\""+page+"\"",
						slashreplace="\""+FILENAME_SLASH_REPLACE+"\"",
						spacereplace="\""+FILENAME_SPACE_REPLACE+"\"")
			xmldecl = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
			result.write(htmldest, encoding="utf-8",  xml_declaration=xmldecl),
		except Exception, inst:
			print "Error: %s" % inst
			return

def main():
	global outdir_src, outdir_html, outdir_attachments, verbose
	usage = "usage: %prog [options] [PAGE1 PAGE2 ...]"
	parser = OptionParser(usage=usage)
	parser.add_option("-q", "--quiet",
	                  action="store_false", dest="verbose", default=True,
	                  help="don't print status messages")
	parser.add_option("--source",
	                  action="store_true", dest="src",
	                  help="download html pages")
	parser.add_option("--html",
	                  action="store_true", dest="html",
	                  help="download wiki source")
	parser.add_option("--clean",
	                  action="store_true", dest="clean",
	                  help="only clean existing html")
	parser.add_option("--att",
	                  action="store_true", dest="att",
	                  help="download attachments")
	parser.add_option("--all",
	                  action="store_true", dest="all",
	                  help="download everything [DEFAULT]")
	(options, args) = parser.parse_args()
	if options.all and (options.src or options.html or options.att or options.clean):
		parser.error("options --all and --source/--html/--att are mutually exclusive")
	if not (options.src or options.html or options.att or options.clean):
		options.all = True
	verbose = options.verbose
	
	# Load settings and init XMLRPC server
	(user,password,server,module) = loadcredentials()
	try:
		if server=='':
			server = raw_input('Server URL:')
		if user=='':
			user = raw_input('User Name:')
		if password=='':
			password = getpass.getpass()
	except:
		sys.exit()
		
	initserver(server+'/'+module,user,password)
	tracpages = loaddefaulttracpages()

	# Get page list
	pages = args
	if not pages:
		try:
			pages = xmlrpc.wiki.getAllPages()
		except Exception, inst:
			print "Error: %s" % inst
			return

	# Download content
	if options.all or options.src:
		forPages(pages,downloadSource,outdir_src,tracpages)
	if options.all or options.html:
		forPages(pages,downloadHtml,outdir_html,tracpages)
	if options.all or options.html or options.clean:
		cleanhtml(pages,outdir_html,server,module,tracpages)
	if options.all or options.att:
		forPages(pages,downloadAttachments,outdir_att,tracpages)


if __name__ == '__main__':
	try:
		main()
	except KeyboardInterrupt:
		sys.exit()
