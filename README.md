![TrakHound DataServer](dataserver-logo-02-75px.png)
<br>
<br>
TrakHound DataServer is used to receive MTConnect® data streamed from TrakHound DataClients and store that data in a database.

TrakHound DataClient and DataServer are designed specifically to store MTConnect® data in a database. Nearly all MTConnect data is stored with its original terminology in database tables for data storage or to use with cloud applications. 

![TrakHound Diagram](TrakHound-Diagram-01.jpg)

# TrakHound
The TrakHound DataClient and DataServer applications provide the manufacturing community with a Free and Open Source alternative so anyone can start collecting valuable machine data that can be used to analyze and improve future production in the upcoming years that will dominated by the IIoT. TrakHound provides you the tools to collect MTConnect data in near raw form and to store that data for later use. Even if you don't see the need for this data now, you may in several years and will wish you had previous year's data to compare. **Take the steps today to prepare for tomorrow and get started with IIoT with TrakHound!**

# Features
- Stores MTConnect data into a database
- Combines MTConnect Agent data into a single data location
- Isolates machine networks so that client applications only need to access the PC running the DataServer
- Data stored in the database can be accessed directly using applications such as Microsoft Access, Crystal Reports, etc.
- Uses pluggable modules for database communications

### Data Storage
**MTConnect Agents by themselves are not storage applications.** This is made clear in the MTConnect Standard. Instead the purpose of MTConnect Agents is to serve data to client applications when requested. While the Agent does keep a small buffer, this buffer is not intended to be used for data storage but rather to retain data between connection interruptions. TrakHound fulfills the role of requesting this data and then storing it in a database for permanant storage. Data is stored which can then be accessed by other TrakHound applications, ERP/MES systems, third party software, or by reading the database directly using software such as Microsoft Access.

### Cloud Applications
Although the MTConnect Agent is a server application itself, most situations require Incoming connections where the application accesses the Agent directly which requires firewall exceptions and since many Agents run on the machine contol itself this would mean each machine would need to accessible from outside networks (usually undesirable for security reasons). TrakHound solves this issue by centralizing the data onto a single server which can either be accessed using the TrakHound API over HTTP/HTTPS or directly to the database itself. Since all of the MTConnect data is now **Outgoing** as opposed to Incoming, machine controls can stay isolated from external networks while a single DataServer accepts incoming requests.

### Security
One of the main goals of TrakHound is to provide tools to securely collect data so that no matter what restrictions your industry requires, you can still benefit from data analysis to improve your manufacturing processes. TrakHound fully supports SSL(TLS) encrypted connections for the DataClient -> DataServer connections as well as the API access. When used with a trusted SSL Certificate, data is sent securely just as online banking/payments are sent. 

By centralizing the point where data is accessed, TrakHound also allows internal machine networks to stay isolated from external networks to prevent both unauthorized data access and possible viruses from effecting the machine controls themselves.

Each TrakHound DataClient can also filter data to only send specific data to certain DataServers. This can be used to only send status data to a cloud server used for machine status monitoring, while sending ALL data to a secure onsite server. 

Of course, the biggest security benefit to using TrakHound is that it is Open Source and the source code can be reviewed to insure exactly what data is being collected and to make sure that no other data is being sent anywhere it shouldn't be.


# Configuration
Configuration for the DataServer is read from the **server.config** XML file in the following format:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<DataServer port="8472" clientTimeout="15000">
  
  <!--SSL Configuration-->
  <SslCertificatePath>C:\Users\Demo\Documents\certificate.pfx</SslCertificatePath>
  <SslCertificatePassword>pwd</SslCertificatePassword>
 
  <!--Client Endpoints that this server will accept or deny connections from-->
  <EndPoints>
    <Allow>
      <EndPoint>192.168.1.10</EndPoint>
      <EndPoint>192.168.1.11</EndPoint>
      <EndPoint>192.168.1.13</EndPoint>
    </Allow>
    <Deny>
      <EndPoint>192.168.1.12</EndPoint>
    </Deny>
  </EndPoints>
    
  <!--Define the path to the database configuration file-->
  <DatabaseConfigurationPath>mysql.config</DatabaseConfigurationPath>
    
</DataServer>
```

#### Port
###### *(XmlAttribute : port)*
The port to listen for incoming streaming requests on

#### Client Timeout
###### *(XmlAttribute : clientTimeout)*
The timeout values (in milliseconds) that the DataServer will wait for activity from a DataClient connection before closing the connection. Setting this value too low can cause the DataClient to create a new connection too frequently. Setting this value too high can cause the DataServer to keep too many idle connections open.

