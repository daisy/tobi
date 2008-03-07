.dp-highlighter
{
	font-family: "Consolas", "Courier New", Courier, mono, serif;
	font-size: 12px;
	background-color: #E7E5DC;
	width: 99%;
	overflow: auto;
	margin: 18px 0 18px 0 !important;
	padding-top: 1px; /* adds a little border on top when controls are hidden */
}

/* clear styles */
.dp-highlighter ol,
.dp-highlighter ol li,
.dp-highlighter ol li span 
{
	margin: 0;
	padding: 0;
	border: none;
}

.dp-highlighter a,
.dp-highlighter a:hover
{
	background: none;
	border: none;
	padding: 0;
	margin: 0;
}

.dp-highlighter .bar
{
	padding-left: 45px;
}

.dp-highlighter.collapsed .bar,
.dp-highlighter.nogutter .bar
{
	padding-left: 0px;
}

.dp-highlighter ol
{
	list-style: decimal; /* for ie */
	background-color: #fff;
	margin: 0px 0px 1px 45px !important; /* 1px bottom margin seems to fix occasional Firefox scrolling */
	padding: 0px;
	color: #5C5C5C;
}

.dp-highlighter.nogutter ol,
.dp-highlighter.nogutter ol li
{
	list-style: none !important;
	margin-left: 0px !important;
}

.dp-highlighter ol li,
.dp-highlighter .columns div
{
	list-style: decimal-leading-zero; /* better look for others, override cascade from OL */
	list-style-position: outside !important;
	border-left: 3px solid #6CE26C;
	background-color: #F8F8F8;
	color: #5C5C5C;
	padding: 0 3px 0 10px !important;
	margin: 0 !important;
	line-height: 14px;
}

.dp-highlighter.nogutter ol li,
.dp-highlighter.nogutter .columns div
{
	border: 0;
}

.dp-highlighter .columns
{
	background-color: #F8F8F8;
	color: gray;
	overflow: hidden;
	width: 100%;
}

.dp-highlighter .columns div
{
	padding-bottom: 5px;
}

.dp-highlighter ol li.alt
{
	background-color: #FFF;
	color: inherit;
}

.dp-highlighter ol li span
{
	color: black;
	background-color: inherit;
}

/* Adjust some properties when collapsed */

.dp-highlighter.collapsed ol
{
	margin: 0px;
}

.dp-highlighter.collapsed ol li
{
	display: none;
}

/* Additional modifications when in print-view */

.dp-highlighter.printing
{
	border: none;
}

.dp-highlighter.printing .tools
{
	display: none !important;
}

.dp-highlighter.printing li
{
	display: list-item !important;
}

/* Styles for the tools */

.dp-highlighter .tools
{
	padding: 3px 8px 3px 10px;
	font: 9px Verdana, Geneva, Arial, Helvetica, sans-serif;
	color: silver;
	background-color: #f8f8f8;
	padding-bottom: 10px;
	border-left: 3px solid #6CE26C;
}

.dp-highlighter.nogutter .tools
{
	border-left: 0;
}

.dp-highlighter.collapsed .tools
{
	border-bottom: 0;
}

.dp-highlighter .tools a
{
	font-size: 9px;
	color: #a0a0a0;
	background-color: inherit;
	text-decoration: none;
	margin-right: 10px;
}

.dp-highlighter .tools a:hover
{
	color: red;
	background-color: inherit;
	text-decoration: underline;
}

/* About dialog styles */

.dp-about { background-color: #fff; color: #333; margin: 0px; padding: 0px; }
.dp-about table { width: 100%; height: 100%; font-size: 11px; font-family: Tahoma, Verdana, Arial, sans-serif !important; }
.dp-about td { padding: 10px; vertical-align: top; }
.dp-about .copy { border-bottom: 1px solid #ACA899; height: 95%; }
.dp-about .title { color: red; background-color: inherit; font-weight: bold; }
.dp-about .para { margin: 0 0 4px 0; }
.dp-about .footer { background-color: #ECEADB; color: #333; border-top: 1px solid #fff; text-align: right; }
.dp-about .close { font-size: 11px; font-family: Tahoma, Verdana, Arial, sans-serif !important; background-color: #ECEADB; color: #333; width: 60px; height: 22px; }

/* Language specific styles */

.dp-highlighter .comment, .dp-highlighter .comments { color: #008200; background-color: inherit; }
.dp-highlighter .string { color: blue; background-color: inherit; }
.dp-highlighter .keyword { color: #069; font-weight: bold; background-color: inherit; }
.dp-highlighter .preprocessor { color: gray; background-color: inherit; }


<?cs
##################################################################
# Site CSS - Place custom CSS, including overriding styles here.
?>
#content {
 margin-left: 2em;
}
ul {
 list-style: square;
}
h1, h2, h3, h4 {
 font-family: "Trebuchet MS", "Lucida Sans", Lucida, "Lucida Grande", Verdana, Helvetica, Arial, 'Bitstream Vera Sans',sans-serif;
 letter-spacing: 0;
}
h1 {
 font-family: verdana, tahoma, helvetica, arial, sans-serif;
 font-weight: bold;
 color: #6EA437;
 /* 6EA437 */
 /* border-left: 1em solid #C2FF33;
    padding-left: 0.3em;
 */
 border-bottom: 2px solid #FFAD5C;
 margin-bottom: 1.5em;
}
h2 {
 font-family: verdana, tahoma, helvetica, arial, sans-serif;
 font-weight: bold;
 color: #003300;
 margin-top: 1.5em;
/*
 border-left: 1em solid #ffcc66;
 padding-left: 0.3em;
*/
 border-bottom: 1px dashed #B8DB94;
}
h3 {
 font-weight: bold;
}
h2::before {
 content: "\2022\A0";
 display: inline;
}
:link, :visited {
 color: #333399;
 border-bottom: 1px dashed #6699cc;
}
a:link:hover, a:visited:hover {
 border-bottom: 2px solid #FFAD5C;
 background-color: transparent;
}
dl dt :link:hover, dl dt :visited:hover {
 border: none;
 background-color: #eef8ed;
}
table tbody tr td :link:hover, table tbody tr td :visited:hover {
 border: none;
}
.nav li {
 white-space: normal;
}
#mainnav {
 border: none;
 background: #f7f7f7 url(nop.png) 0 0;
 background: white;
 padding-bottom: 0.4em;
 margin: 0;
 margin-top: 1em;
 margin-bottom: 1em;
 border-bottom: .2em solid #B8DB94;
}
#mainnav li {
 font-size: 1.3em;
 padding: 0;
/*
 border-top: 1px dashed #B8DB94;
 padding-top: 0.3em;
 padding-bottom: 0.3em;
*/
}
#mainnav li.first {
 border-left: 1px solid #B8DB94;
}
#mainnav :link, #mainnav :visited {
 background: url(nop.gif) 0 0 no-repeat;
 background: white;
 border: none;
 border-top: 1px dashed #B8DB94;
 border-right: 1px solid #B8DB94;
 padding-left: 0.5em;
 padding-right: 0.5em;
 padding-top: .2em;
 padding-bottom: .2em;
 color: #6B6B6B;
}
#mainnav :link:hover, #mainnav :visited:hover {
 background: #eef8ed;
 color: #333333;
 border: none;
 border-right: 1px solid #B8DB94;
 border-top: 1px solid #6EA437;
}
#mainnav .active :link, #mainnav .active :visited {
 background: url(nop.png) 0 0 repeat-x;
 background: white;
 color: #000000;
 border-top: 2px solid #6EA437;
 border-left: 3px solid #6EA437;
 border-right: 3px solid #6EA437;
 border-bottom: .35em solid white;
 font-weight: normal;
}
#mainnav .active :link:hover, #mainnav .active :visited:hover {
 background: #eef8ed;
 color: #333333;
 border-top: 2px solid #6EA437;
 border-left: 3px solid #6EA437;
 border-right: 3px solid #6EA437;
 border-bottom: .35em solid white;
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
.wiki-toc h4 {
 margin-bottom: 0.7em;
}
div.blog-calendar {
 background: #eef8ed;
 border: 2px solid #B8DB94;
}
div.blog-calendar .missing {
	background: none;
}
tr.blog-calendar-current {
 background: #B8DB94;
}
.wiki-toc {
 background: #eef8ed;
 border: 2px solid #B8DB94;
}
.wiki-toc ol li, .wiki-toc ul li {
 padding: 0.3em;
}
input[type=button], input[type=submit], input[type=reset] {
 background: #eef8ed;
 border: 1px solid #B8DB94;
}
#ticket {
 background: #eef8ed;
 border: 1px solid #B8DB94;
}
#prefs {
 background: #eef8ed;
 border: 1px solid #B8DB94;
}
#changelog {
 border: 1px solid #B8DB94;
}
fieldset.ticketbox {
 border: 1px solid #FFAD5C;
 border-left: 2px solid #FFAD5C;
 border-right: 2px solid #FFAD5C;
}
fieldset.ticketbox legend {
 color: #6B6B6B;
}
table.progress td.closed {
 background: #3399cc;
}
table.progress td.open {
 background: #99cccc;
}
table.progress {
 border: 1px solid #336699;
}