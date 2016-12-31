![TrakHound DataServer](dataserver-logo-100px.png)
<br>
<br>
TrakHound Data Server is used to receive MTConnect® data streamed from TrakHound Data Clients and store that data in a database. Supports SSL for secure connections and uses streaming to minimize connections. Currently supports MySQL databases.

# Configuration
Configuration is read from the **server.conf** XML file in the following format:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<DataServer>
  
  <!--Set Ports for Server Listening-->
  <RestPort>80</RestPort>
  <StreamingPort>8472</StreamingPort>
  
  <!--Set URL Prefixes for REST server-->
  <Prefixes>
    <Prefix>http://localhost/</Prefix>
  </Prefixes>

  <!--SSL Configuration-->
  <SslCertificatePath>C:\Users\Patrick\Documents\test-ssl-certificate\localhost.crt</SslCertificatePath>
  <SslCertificatePassword>snaps</SslCertificatePassword>
  
  <!--Client Connection Inactivity Timeout (milliseconds)-->
  <ClientConnectionTimeout>15000</ClientConnectionTimeout>
  
  <!--Allowed Client Endpoints that this server will accept connections from-->
  <EndPoints>
    <EndPoint>127.0.0.1</EndPoint> 
  </EndPoints>

  <!--Configure the MySQL database connection-->
  <MySql server="192.168.1.13" user="development" password="trakhound" database="trakhound"></MySql>
    
</DataServer>
```

## Device 
Represents each MTConnect Agent that the Device Server going to be reading from.

#### Device ID 
###### *(XmlAttribute : deviceId)*
The globally unique identifier for the device (usually a GUID)

#### Device Name
###### *(XmlAttribute : deviceName)*
The DeviceName of the MTConnect Device to read from

#### Address
###### *(XmlText)*
The base Url of the MTConnect Agent. Do not specify the Device Name in the url, instead specify it under the deviceName attribute.

## Data Server
Represents each TrakHound Data Server that data is sent to in order to be strored and processed.

#### Url 
###### *(XmlAttribute : url)*
The base Url of the TrakHound Data Server to send data to

#### Buffer Path
###### *(XmlAttribute : bufferPath)*
The directory where the buffer files should be stored. The Buffer is used to store data that hasn't been successfully sent yet.

