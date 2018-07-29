<?php

header("Content-Type: text/plain; charset=utf-8");

$point5 = file("0.5.1.1.csv");
$point6 = file("0.6.csv");
$oldtagmap = file("ForgeTagMap_old.csv"); // to know which ones are relevant in 0.5.1.1 forge

$oldindexarray = array();
$objectsthatwerediscarded = array();
$objectsthatwerenotdiscarded = array();
foreach($oldtagmap as $item){
	$parts = explode(",",$item);
	$superoldindex = $parts[0];
	$oldindex = trim($parts[1]);
	$oldindexarray[] = $oldindex;
}


$point6_nametoindex = array();
$point6_indextoname = array();

foreach($point6 as $item){
	
	$parts = explode(",",$item);
	$point6index = substr($parts[0],-4);
	$point6name = trim($parts[1]);
	if(!isset($point6_nametoindex[$point6name])){
		$point6_nametoindex[$point6name] = $point6index;
	}
	$point6_indextoname[$point6index] = $point6name;
}

$point5indextopoint6index = array();

foreach($point5 as $item){
	
	$parts = explode(",",$item);
	$point5index = substr($parts[0],-4);
	$point5name = trim($parts[1]);
	$point6index = isset($point6_nametoindex[$point5name]) ? $point6_nametoindex[$point5name] : NULL;
	if(isset($point6_indextoname[$point5index]) && $point5name == $point6_indextoname[$point5index]){
		
	} 
	else {
		if($point6index !== $point5index){
			$point5indextopoint6index[$point5index] = $point6index;
			
			if(in_array($point5index,$oldindexarray) && $point6index === NULL){
				$objectsthatwerediscarded[] = $point5name;
			}
			if(in_array($point5index,$oldindexarray) && $point6index !== NULL){
				$objectsthatwerenotdiscarded[] = $point5name;
			}
		}
	}
}


// make csv map
$csv = "";
$limitedcsv = "";

foreach($point5indextopoint6index as $point5index => $point6index){
	$csv .= $point5index.",".$point6index."\n";
	
	if(in_array($point5index,$oldindexarray)){
		echo 1;
		$limitedcsv .= $point5index.",".$point6index."\n";
	}
}

file_put_contents("map.csv",$csv);
file_put_contents("map_limited.csv",$limitedcsv);
file_put_contents("discardedobjects.csv",implode("\n",$objectsthatwerediscarded));
file_put_contents("not-discardedobjects.csv",implode("\n",$objectsthatwerenotdiscarded));
