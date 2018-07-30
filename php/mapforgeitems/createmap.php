<?php

header("Content-Type: text/plain; charset=utf-8");

$point5 = file("0.5.1.1.csv");
$point6 = file("0.6.csv");
$oldtagmap = file("ForgeTagMap_old.csv"); // to know which ones are relevant in 0.5.1.1 forge

$point6forgeitemsXML = file_get_contents("items.xml");
$point6forgeitemsParsed = simplexml_load_string($point6forgeitemsXML) or die("Error: Cannot create XML");

// All items in 0.6 forge: 
$point6itemsUnfiltered = $point6forgeitemsParsed->xpath('//item');
$filteredpoint6Items = array();
foreach($point6itemsUnfiltered as $item){
	if(@$item["tagindex"]){
		preg_match('/0x0*([0-9a-f]+)/i', (string)$item["tagindex"], $result);
		$tagindex = strtoupper($result[1]);
		$filteredpoint6Items[$tagindex] = array("tagid"=>$tagindex,"type"=>(string)$item["type"],"name"=>(string)$item["name"]);
	}
}

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
	if(isset($filteredpoint6Items[$point6index])){
		$filteredpoint6Items[$point6index]["point6name"] = $point6name;
	}
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
	if(isset($filteredpoint6Items[$point5index])){
		$filteredpoint6Items[$point5index]["point5name"] = $point5name;
	}
	$point6index = isset($point6_nametoindex[$point5name]) ? $point6_nametoindex[$point5name] : NULL;
	if(isset($point6_indextoname[$point5index]) && $point5name == $point6_indextoname[$point5index]){
		if(isset($filteredpoint6Items[$point5index])){
			unset($filteredpoint6Items[$point5index]);
		}
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

print_r($filteredpoint6Items);

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


$point6forgeitemsCSV = "";
foreach($filteredpoint6Items as $index=>$item){
	$point6forgeitemsCSV .= "1,$index,".$item["type"].",".str_replace(",","_",$item["name"]).",".(isset($item["point5name"]) ? $item["point5name"] : null).",".(isset($item["point6name"]) ? $item["point6name"] : null)."\n";
}


file_put_contents("map.csv",$csv);
file_put_contents("point6forgeitems.csv",$point6forgeitemsCSV);
file_put_contents("map_limited.csv",$limitedcsv);
file_put_contents("discardedobjects.csv",implode("\n",$objectsthatwerediscarded));
file_put_contents("not-discardedobjects.csv",implode("\n",$objectsthatwerenotdiscarded));
