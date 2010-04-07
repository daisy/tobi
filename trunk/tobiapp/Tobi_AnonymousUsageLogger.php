<?php

$ip = isset($_SERVER['HTTP_X_FORWARDED_FOR']) ? $_SERVER['HTTP_X_FORWARDED_FOR'] : $_SERVER['REMOTE_ADDR'];
$date = date("Y-m-d");

if (isset($_GET['debug']))
{
echo("<h2>IP: $ip</h2>");
echo("<h2>DATE: $date</h2>");
}
else
{
echo("\nIP: $ip");
echo("\nDATE: $date");
}

$log_filename = "./xxx/xxx_$ip"."_"."$date.txt";

$enableFileLog = isset($_GET['debug']);

if (isset($_GET['debug']))
{
echo("<h3>Opening log file</h3>");
}

if($enableFileLog)
{
$log_file = fopen($log_filename, 'a') or die("Can't open log file for writing !"); // append
fwrite($log_file, "\n------");
}

if (isset($_GET['debug']))
{
echo("<h3>Logging usage data:</h3>");
}

// ----- TIME

$text_raw = date("H:i:s O");

if (isset($_GET['debug']))
{
$text_html = "<p><strong>TIME:</strong> $text_raw</p>";
echo $text_html;
}
else
{
echo("\nTIME: $text_raw");
}

if($enableFileLog)
{
$text_file = "\n$text_raw";
fwrite($log_file, $text_file);
}

echo "<p>";
print_r($_GET);
//var_dump($_GET);
echo "</p>";

if($enableFileLog)
{
fwrite($log_file, "\n---");
foreach($_GET as $name => $value) {
	fwrite($log_file, "\n$name=$value");
}
}

if (isset($_GET['debug']))
{
echo("<h3>Closing log file</h3>");
}

if($enableFileLog)
{
fclose($log_file);
}
?>