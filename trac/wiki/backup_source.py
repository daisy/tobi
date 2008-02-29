#!/usr/bin/env python
# encoding: utf-8

# Good source of help: http://www.fieldguidetoprogrammers.com/blog/python/feedextractor-a-quick-and-dirty-python-script-to-grab-lots-of-feeds-from-web-pages/

from BeautifulSoup import BeautifulSoup
import urllib2
from xml.dom import minidom
from urlparse import urlparse
import socket
import os

timeout = 30
socket.setdefaulttimeout(timeout)

# urlparse ==> <scheme>://<netloc>/<path>;<params>?<query>#<fragment>

rooturl = 'http://daisy-trac.cvsdude.com'
wikiroot = '/tobi/wiki/'
baseurl = rooturl + wikiroot + 'TitleIndex'

FILENAME_SLASH_REPLACE = '%SLASH%'
outputdir = './source/'

tracwikiwords = []

def gethtml(url):
	print 'Getting URL: [' + url + ']...'
	html = urllib2.urlopen(url).read()
	return html

def loaddefaulttracpages():
	global tracwikiwords
	filename = 'defaulttracpages.xml'
	print 'Loading file: [' + filename + ']...'
	dom = minidom.parse(filename)
	for node in dom.getElementsByTagName('tracwikiword'):
		tracwikiwords.append(node.attributes['name'].value)
	print 'Loaded [' + `len(tracwikiwords)` + '] Trac default wiki pages.'

def extractlinks(html):
	global tracwikiwords,baseurl,rooturl,wikiroot
	print 'Parsing the HTML, searching for links...'
	soup = BeautifulSoup(html)
	anchors = soup.findAll('a')
	print 'Found [' + `len(anchors)` + '] <a href="..."> links.'
	links = []
	for a in anchors:
		if a.has_key('href'):
			str = a['href']
			if str.startswith(wikiroot):
				urlstr = rooturl + str
				print 'Parsing URL: [' + urlstr + ']...'
				o = urlparse(urlstr)
				if len([s for s in tracwikiwords if s in o.path]) == 0 and len([s for s in links if o.path in s]) == 0:
					url = rooturl + str + '?format=txt'
					wikiword = str[len(wikiroot):]
					print 'Adding link to download: [' + wikiword + ' %% ' + url + '].'
					links.append({'wikiword':wikiword,'url':url})
	print 'Extracted [' + `len(links)` + '] relevant links (excluding Trac\'s default pages).'
	return links

def downloadpages(links):
	global FILENAME_SLASH_REPLACE,outputdir
	print 'Downloading [' + `len(links)` + '] Trac wiki pages...'
	#not os.access(outputdir, os.F_OK):
	if not os.path.isdir(outputdir):
		os.mkdir(outputdir)
	ext = '.tracwiki.txt'
	for link in links:
		filename = outputdir + link['wikiword'].replace('/',FILENAME_SLASH_REPLACE) + ext
		print 'Creating file [' + filename + ']...'
		fp = open(filename,"w")
		url = link['url']
		print 'Reading URL [' + url + ']...'
		source = urllib2.urlopen(url).read()
		
		#TODO: analyze source for image attachments + download actual image (should work with any binary, really)
		#e.g. [[Image(TOBI_04.png)]]
		#http://daisy-trac.cvsdude.com/tobi/attachment/wiki/inception/Mockups/TOBI_04.png?format=raw
		
		print 'Writing file [' + filename + ']...'
		fp.write(source)
		fp.close()

def main():
	global tracwikiwords,baseurl
	loaddefaulttracpages()
	html = gethtml(baseurl)
	links = extractlinks(html)
	downloadpages(links)

if __name__ == '__main__':
	main()
