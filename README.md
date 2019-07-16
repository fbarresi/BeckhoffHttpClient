Beckhoff Http Client
======
[![Build status](https://ci.appveyor.com/api/projects/status/bhsi49foyc8tnve2?svg=true)](https://ci.appveyor.com/project/fbarresi/beckhoffhttpclient)
![Licence](https://img.shields.io/github/license/fbarresi/Beckhoffhttpclient.svg)

Unofficial TwinCAT function for HTTP requests with json conversion

This open source library allow any beckhoff to make API requests with an HTTP/HTTPS client.
If you are going to buy the [TF6760 | TC3 IoT HTTPS/REST](https://www.beckhoff.com.ph/default.asp?twincat/tf6760.htm) (planned for End 2019, not realized yet) you should first read this page and wonder how open-source software can simplify your life.

## Key features

- **NO Licence** costs (This software is free also for commercial uses)
- support HTTP/HTTPS API calls
- support **all** API methods (GET, POST, PUT, DELETE, HEAD, OPTIONS, PATCH, MERGE, COPY) 
- reads any Json body with complex nested structures
- parse any Json response to plc structure

## Prepare your PLC

- Install the _unofficial_ TwinCAT Function [TFU001](https://github.com/fbarresi/BeckhoffHttpClient/releases/latest) on your target system
- Install the TwinCAT Library to your project

## How to use the TwinCAT Library

### Reference the BeckhoffHttpClient Library

Download and reference the [BeckhoffHttpClient library](https://github.com/fbarresi/BeckhoffHttpClient/releases/latest) and import it to your project.

![](https://github.com/fbarresi/BeckhoffHttpClient/raw/master/doc/images/BeckhoffHttpClientLibrary.png)

You can now declare and call a Client in your program and start using rest API.

```
PROGRAM MAIN
VAR
	client : HttpClient;
END_VAR
```
```
client(
	Execute:=FALSE , 
	Address:= 'https://dog.ceo/api/breeds/image/random', 
	CallMethod:= 'GET' , 
	Body:= '', 
	ResponseCode:= 'GVL.ResponseCode', 
	Response:= 'GVL.Response',  
	HasError=> , 
	ErrorId=> );
```

### The JSON Attribute

This software can parse and convert normal DUTs (also nested DUTs) into Json object thaks to the power of [TwinCAT.JsonExtension](https://github.com/fbarresi/TwinCAT.JsonExtension).
The only things you have to do is to add the JSON attribute to your code like follows and specify if your field has another json-name or can be used with its own name.

```
TYPE JsonDUT :
STRUCT
	{attribute 'json' := 'message'}
	sMessage : STRING;
	iResponse : INT;
	{attribute 'json' := 'status'}
	sStatus : STRING;
	{attribute 'json' := 'numbers'}
	daNumbers : ARRAY[1..10] OF DINT := [1,2,3,4,5,6,7,8,9,10];
	{attribute 'json'}
	child : ChildDUT;
END_STRUCT
END_TYPE
```

## Planned features

- custom request header
- API keys

## Would you like to contribute?

Yes, please!

Try the library and feel free to open an issue or ask for support. 

Don't forget to **star this project**! 
