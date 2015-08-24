Set WshShell = WScript.CreateObject("WScript.Shell") 

If WScript.Arguments(0) <> "" Then     
WshShell.AppActivate(WScript.Arguments(0))
    WshShell.SendKeys "{F5}"
    WScript.Sleep(100)
End If
If WScript.Arguments(2) <> "" Then
    WshShell.AppActivate(WScript.Arguments(2))
    WshShell.SendKeys "{F5}"
    WScript.Sleep(100)
End If
If WScript.Arguments(3) <> "" Then
    WshShell.AppActivate(WScript.Arguments(3))
    WshShell.SendKeys "{F5}"
    WScript.Sleep(100)
End If
WshShell.AppActivate(WScript.Arguments(1))
WScript.Sleep(100)

