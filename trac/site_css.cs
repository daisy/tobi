<?cs
##################################################################
# Site CSS - Place custom CSS, including overriding styles here.
?>

h1, h2, h3, h4 {
 font-family: "Trebuchet MS", "Lucida Sans", Lucida, "Lucida Grande", Verdana, Helvetica, Arial, 'Bitstream Vera Sans',sans-serif;
 letter-spacing: 0;
}
h1 {
 background: #eed;
 border-left: 1em solid #6EA437;
 border-bottom: 1px solid #6EA437;
 margin-bottom: 1.4em;
 padding-left: 0.3em;
}
h2 {
 border-left: 1em solid #B8DB94;
 border-bottom: 1px dashed #B8DB94;
 padding-left: 0.3em;
}
:link, :visited {
 color: #376D37;
}
.nav li {
 white-space: normal;
}
#mainnav {
 border: none;
 background: #f7f7f7 url(nop.png) 0 0;
 padding-bottom: 0.4em;
 /* margin: .66em 0 .33em; */
 margin: 0;
 margin-top: 1em;
 margin-bottom: 1em;
 /* border-bottom: 1px dashed #B8DB94; */
 border-bottom: .2em solid #B8DB94;
}
#mainnav li {
 font-size: 1.3em;
 border-top: 1px dashed #B8DB94;
 /* border-bottom: 2px dashed #B8DB94; */
 padding: 0;
 padding-top: 0.3em;
 padding-bottom: 0.3em;
 border-right: 1px solid #B8DB94;
}
#mainnav li.first {
 border-left: 1px solid #B8DB94;
}
#mainnav :link, #mainnav :visited {
 background: url(nop.gif) 0 0 no-repeat;
 border: none;
 /* border-left: 1px solid #B8DB94; */
 padding-left: 0.5em;
 padding-right: 0.5em;
 padding-top: .2em;
 padding-bottom: .2em;
}
#mainnav :link:hover, #mainnav :visited:hover {
 background: #6EA437;
 color: #ffffff;
 border: none;
 /* border-left: 1px solid #B8DB94; */
}
#mainnav .active :link, #mainnav .active :visited {
 background: url(nop.png) 0 0 repeat-x;
 background: #eed;
 color: #333333;
 border: 2px solid #6EA437;
 border-bottom: .4em solid white;
}
#mainnav .active :link:hover, #mainnav .active :visited:hover {
 background: #6EA437;
 color: #ffffff;
 border: 2px solid #6EA437;
 border-bottom: .4em solid white;
}
dt em {
 color: #6EA437;
}
.milestone .info h2 em {
 color: #6EA437;
}
h1 :link, h1 :visited {
 color: #6EA437
}
.wiki-toc .active {
 background: #B8DB94;
}
div.blog-calendar {
 background: #eed;
 border: 1px solid #B8DB94;
 border-right: 0.5em solid #B8DB94;
}
div.blog-calendar .missing {
	background: none;
}
tr.blog-calendar-current {
 background: #B8DB94;
}
.wiki-toc {
 background: #eed;
 border: 1px solid #B8DB94;
 border-right: 0.5em solid #B8DB94;
}
.wiki-toc ol li, .wiki-toc ul li {
 padding: 0.3em;
}
input[type=button], input[type=submit], input[type=reset] {
 background: #eed;
 border: 1px solid #B8DB94;
}
#ticket {
 background: #eed;
 border: 1px solid #B8DB94;
}
#prefs {
 background: #eed;
 border: 1px solid #B8DB94;
}
#changelog {
 border: 1px solid #B8DB94;
}
fieldset.ticketbox {
 border: 1px solid #B8DB94;
 border-left: 2px solid #B8DB94;
 border-right: 2px solid #B8DB94;
}
fieldset.ticketbox legend {
 color: #6EA437;
}
table.progress td.closed {
  background: #fd8;
}
