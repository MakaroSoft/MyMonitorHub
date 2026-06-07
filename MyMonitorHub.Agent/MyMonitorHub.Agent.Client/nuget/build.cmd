mkdir lib\net40
mkdir tools\net40
mkdir content

copy ..\bin\debug\MyMonitorHub.Agent.Client* lib\net40\

nuget pack