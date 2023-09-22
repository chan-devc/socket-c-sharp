Set objects = WScript.CreateObject("WScript.Shell")
appPath = "C:\Users\chan\Documents\socket-c-sharp"
objects.CurrentDirectory = appPath
objects.Run "cmd /c dotnet run", 0, True