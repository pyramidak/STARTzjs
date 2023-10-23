Imports System.Globalization
Imports System.Runtime.InteropServices

#Region " Enums "

Public Enum DiskTypes
    System_0 = 0
    Floppy_1 = 1
    Removable_2 = 2
    Fixed_3 = 3
    Server_4 = 4
    Cdrom_5 = 5
    Ramdisk_6 = 6
    Harddisk_7 = 7
    Flashdisk_8 = 8
End Enum

Enum HKEY
    CLASSES_ROOT = 0
    CURRENT_CONFIG = 1
    CURRENT_USER = 2
    LOCALE_MACHINE = 4
    PERFORMANCE_DATA = 5
    USERS = 6
End Enum

#End Region

#Region " Register "

Class myRegister

    Public Const Run As String = "Software\Microsoft\Windows\CurrentVersion\Run"

#Region "Procedures "

    Private Shared Function GetRegKey(lngRoot As HKEY) As Microsoft.Win32.RegistryKey
        Select Case CInt(lngRoot)
            Case 0 'HKEY_CLASSES_ROOT
                GetRegKey = Microsoft.Win32.Registry.ClassesRoot
            Case 1 'HKEY_CURRENT_CONFIG
                GetRegKey = Microsoft.Win32.Registry.CurrentConfig
            Case 2 'HKEY_CURRENT_USER
                GetRegKey = Microsoft.Win32.Registry.CurrentUser
            Case 5 'HKEY_PERFORMANCE_DATA
                GetRegKey = Microsoft.Win32.Registry.PerformanceData
            Case 6 'HKEY_USERS
                GetRegKey = Microsoft.Win32.Registry.Users
            Case Else 'HKEY_LOCALE_MACHINE = 4
                GetRegKey = Microsoft.Win32.Registry.LocalMachine
        End Select
    End Function

    Public Shared Function DoesKeyExist(lngRootKey As HKEY, strKey As String) As Boolean
        Dim objRegKey As Microsoft.Win32.RegistryKey
        Dim bOK As Boolean

        objRegKey = GetRegKey(lngRootKey)
        Try
            objRegKey = objRegKey.OpenSubKey(strKey, False)
        Catch
        End Try

        If objRegKey Is Nothing Then
            bOK = False
        Else
            bOK = True
        End If
        If Not objRegKey Is Nothing Then
            objRegKey.Close()
            objRegKey = Nothing
        End If

        DoesKeyExist = bOK
    End Function
#End Region

#Region "Create "

    Public Shared Function CreateKey(lngrootkey As HKEY, strKey As String) As Boolean
        CreateKey = False
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            objRegKey = objRegKey.CreateSubKey(strKey)
            If objRegKey IsNot Nothing Then
                CreateKey = True
                objRegKey.Close()
            End If
        Catch
        End Try
    End Function

    Public Shared Function CreateValue(lngrootkey As HKEY, strKey As String, strValName As String, objVal As Object) As Boolean
        CreateValue = False
        If DoesKeyExist(lngrootkey, strKey) = False Then
            If CreateKey(lngrootkey, strKey) = False Then Exit Function
        End If

        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            objRegKey = objRegKey.OpenSubKey(strKey, True)
            If objRegKey IsNot Nothing Then
                objRegKey.SetValue(strValName, objVal, If(IsNumeric(objVal), Microsoft.Win32.RegistryValueKind.DWord, Microsoft.Win32.RegistryValueKind.String))
                objRegKey.Flush()
                CreateValue = True
                objRegKey.Close()
            End If
        Catch
        End Try
    End Function

#End Region

#Region "Delete "

    Public Shared Function DeleteKey(lngrootkey As HKEY, strKey As String, Optional bRecursive As Boolean = False) As Boolean
        DeleteKey = False
        If DoesKeyExist(lngrootkey, strKey) = False Then Exit Function
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            If objRegKey.OpenSubKey(strKey).SubKeyCount > 0 Then
                objRegKey.DeleteSubKeyTree(strKey)
                DeleteKey = True
            Else
                objRegKey.DeleteSubKey(strKey)
                DeleteKey = True
            End If
            objRegKey.Close()
        Catch
        End Try
    End Function

    ' Registry-Wert löschen (Schlüssel muss existieren) 
    Public Shared Function DeleteValue(lngrootkey As HKEY, strKey As String, strValName As String) As Boolean
        DeleteValue = False
        If strValName = "" Then Exit Function
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            objRegKey = objRegKey.OpenSubKey(strKey, True)
            If objRegKey IsNot Nothing Then
                objRegKey.DeleteValue(strValName)
                objRegKey.Flush()
                DeleteValue = True
                objRegKey.Close()
            End If
        Catch
        End Try
    End Function
#End Region

#Region "Query "

    Public Shared Function GetValue(lngrootkey As HKEY, strKey As String, strValName As String, objDefault As String) As String
        Return QueryValue(lngrootkey, strKey, strValName, objDefault).ToString
    End Function

    Public Shared Function GetValue(lngrootkey As HKEY, strKey As String, strValName As String, objDefault As Integer) As Integer
        Dim obj As Object = QueryValue(lngrootkey, strKey, strValName, objDefault)
        If IsNumeric(obj) Then
            Return CInt(obj)
        Else
            Return objDefault
        End If
    End Function

    Public Shared Function GetValue(lngrootkey As HKEY, strKey As String, strValName As String, objDefault As Boolean) As Boolean
        Dim obj As Object = QueryValue(lngrootkey, strKey, strValName, objDefault)
        If IsNumeric(obj) AndAlso CInt(obj) = 0 Or CInt(obj) = 1 Then
            Return CBool(obj)
        Else
            Return objDefault
        End If
    End Function

    Private Shared Function QueryValue(lngrootkey As HKEY, strKey As String, strValName As String, objDefault As Object) As Object
        QueryValue = objDefault
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            objRegKey = objRegKey.OpenSubKey(strKey)
            If objRegKey IsNot Nothing Then
                QueryValue = objRegKey.GetValue(strValName, objDefault)
                objRegKey.Close()
            End If
        Catch
        End Try
    End Function

    Public Shared Function QueryNames(lngrootkey As HKEY, strKey As String) As String()
        Dim Nic(0) As String
        QueryNames = Nic
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            objRegKey = objRegKey.OpenSubKey(strKey)
            If objRegKey IsNot Nothing Then
                QueryNames = objRegKey.GetValueNames()
                objRegKey.Close()
            End If
        Catch
        End Try
    End Function

    Public Shared Function QueryKeys(lngrootkey As HKEY, strKey As String) As String()
        Dim Nic(0) As String
        QueryKeys = Nic
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        Try
            objRegKey = objRegKey.OpenSubKey(strKey)
            If objRegKey IsNot Nothing Then
                QueryKeys = objRegKey.GetSubKeyNames()
                objRegKey.Close()
            End If
        Catch
        End Try
    End Function
#End Region

#Region "Find "

    Public Shared Function FindValue(lngrootkey As HKEY, strKey As String, strVal As String) As String
        Dim objRegKey As Microsoft.Win32.RegistryKey

        objRegKey = GetRegKey(lngrootkey)
        FindValue = ""
        Try
            objRegKey = objRegKey.OpenSubKey(strKey)
            If objRegKey Is Nothing Then
                Exit Function
            Else
                For Each oneValueName As String In objRegKey.GetValueNames
                    Dim defValue As String = ""
                    If LCase(CStr(objRegKey.GetValue(oneValueName, defValue))).Contains(LCase(strVal)) Then
                        FindValue = oneValueName
                        Exit For
                    End If
                Next
            End If
            objRegKey.Close()
        Catch
        End Try
    End Function
#End Region

#Region "MyApp "

    Public Shared Function GetCloudMyApp() As Cloud
        Return CType(myRegister.GetValue(HKEY.CURRENT_USER, "Software\pyramidak\" & Application.ExeName, "Cloud", 0), Cloud)
    End Function

    Public Shared Sub WriteCloudMyApp(cCloud As Cloud)
        myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak\" & Application.ExeName, "Cloud", cCloud)
    End Sub

    Public Shared Function GetTypeMyApp() As Integer
        Return myRegister.GetValue(HKEY.CURRENT_USER, "Software\pyramidak\" & Application.ExeName, "Type", 1)
    End Function

    Public Shared Sub WriteTypeMyApp(type As Integer)
        myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak\" & Application.ExeName, "Type", type)
    End Sub

    Public Shared Function GetAutoUpdateMyApp() As Boolean
        Return myRegister.GetValue(HKEY.CURRENT_USER, "Software\pyramidak\" & Application.ExeName, "AutoUpdate", True)
    End Function

    Public Shared Sub WriteAutoUpdateMyApp(doupdate As Boolean)
        myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak\" & Application.ExeName, "AutoUpdate", doupdate)
    End Sub

    Public Shared Function GetAutoStartMyApp() As Boolean
        Return If(myRegister.GetValue(HKEY.CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Application.ExeName, "").ToLower = Chr(34) & System.Reflection.Assembly.GetExecutingAssembly().Location.ToLower & Chr(34) & " -win", True, False)
    End Function

    Public Shared Sub WriteAutoStartMyApp()
        myRegister.CreateValue(HKEY.CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Application.ExeName, Chr(34) & System.Reflection.Assembly.GetExecutingAssembly().Location & Chr(34) & " -win")
    End Sub

    Public Shared Sub DeleteAutoStartMyApp()
        myRegister.DeleteValue(HKEY.CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Application.ExeName)
    End Sub

#End Region

End Class

#End Region

#Region " String "

Class myString

    ' Convert a string to a byte array
    Public Shared Function ToBytes(ByVal Text As String) As Byte()
        Dim encoding As New Text.ASCIIEncoding()
        Return encoding.GetBytes(Text)
    End Function

    ' Convert a byte array to a string:
    Public Shared Function FromBytes(ByVal arrBytes() As Byte) As String
        Dim Text As String = ""
        For i As Integer = LBound(arrBytes) To UBound(arrBytes)
            Text = Text & Chr(arrBytes(i))
        Next
        Return Text
    End Function

    Public Shared Function FromFile(filename As String) As String
        FromFile = ""
        If myFile.Exist(filename) = False Then Exit Function
        Try
            Dim tr As IO.TextReader = New IO.StreamReader(filename)
            FromFile = tr.ReadToEnd
            tr.Dispose()
        Catch
        End Try
    End Function

    Public Shared Function FromEmbeddedResource(filename As String) As String
        Dim stream As IO.Stream = Reflection.Assembly.GetExecutingAssembly.GetManifestResourceStream("RootSpace." & filename)
        Return FromStream(stream)
    End Function

    Public Shared Function FromStream(stream As IO.Stream, Optional size As Long = 0) As String
        stream.Seek(0, IO.SeekOrigin.Begin)
        Dim Reader As New IO.BinaryReader(stream)
        If size = 0 OrElse stream.Length < size Then size = stream.Length
        Dim fileByte() As Byte = Reader.ReadBytes(CInt(size))
        Reader.Close() : stream.Close()
        Return FromBytes(fileByte)
    End Function

    Public Shared Function FirstWord(ByVal Text As String) As String
        Dim iEndWord As Integer = Text.IndexOf(" ")
        If iEndWord = -1 Then
            Return Text
        Else
            Return Text.Substring(0, iEndWord)
        End If
    End Function

    Public Shared Function Left(ByVal Text As String, ByVal Length As Integer) As String
        If Text Is Nothing Then Return ""
        If Text.Length <= Length Then Return Text
        Text = Text.Substring(0, Length - 1)
        Dim cutLink As Integer = Strings.Left(Text, Length - 1).LastIndexOf(" ") + 1
        If cutLink < 2 Then Return Text
        Return Strings.Left(Text, cutLink)
    End Function

    Public Shared Function FromNumber(ByVal Cislo As Integer, ByVal PocetNul As Integer) As String
        Return New String(CChar("0"), PocetNul - Cislo.ToString.Length) + Cislo.ToString
    End Function

    Public Shared Function GetDouble(text As String) As Double
        If text Is Nothing OrElse text = "" Then Return -1
        Dim separator As String = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator
        If separator = "." Then
            text = text.Replace(",", ".")
        Else
            text = text.Replace(".", ",")
        End If

        If IsNumeric(text) Then Return CDbl(text)
        If IsNumeric(text.Substring(0, 1)) Then
            For a As Integer = 1 To text.Length
                If Not text.Substring(a, 1) = separator AndAlso IsNumeric(text.Substring(a, 1)) = False Then
                    Return CDbl(text.Substring(0, a))
                End If
            Next
        End If
        Return -1
    End Function

    Public Shared Function GetDate(text As String) As Date
        If text Is Nothing OrElse text = "" Then Return New Date
        'September 14, 2019 at 02:27PM
        If text.Length > 20 AndAlso text.Substring(text.Length - 10, 2) = "at" Then 'IFTTT date format
            text = text.Replace("at ", "")
            Try
                Return DateTime.ParseExact(text, "MMMM d, yyyy hh:mmtt", CultureInfo.InvariantCulture)
            Catch
                Return New Date
            End Try
        Else
            Try 'system date format
                Return DateTime.Parse(text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault)
            Catch ex As Exception
                Try
                    Return DateTime.Parse(text, New CultureInfo("en-US"), DateTimeStyles.NoCurrentDateDefault)
                Catch
                    Try
                        Return DateTime.Parse(text, New CultureInfo("zh-CN"), DateTimeStyles.NoCurrentDateDefault)
                    Catch
                        Try
                            Return DateTime.Parse(text, New CultureInfo("ru-RU"), DateTimeStyles.NoCurrentDateDefault)
                        Catch
                            Return New Date
                        End Try
                    End Try
                End Try
            End Try
        End If
    End Function

#Region " Crypting "
    ' Encrypt and Decrypta a text
    Private Shared DES As New System.Security.Cryptography.TripleDESCryptoServiceProvider
    Private Shared MD5 As New System.Security.Cryptography.MD5CryptoServiceProvider

    Private Shared Function MD5Hash(ByVal value As String) As Byte()
        Return MD5.ComputeHash(System.Text.UTF8Encoding.ASCII.GetBytes(value))
    End Function

    Public Shared Function Encrypt(ByVal Text As String, ByVal Password As String) As String
        DES.Key = MD5Hash(Password)
        DES.Mode = System.Security.Cryptography.CipherMode.ECB

        Dim UTF8 As New System.Text.UTF8Encoding
        Dim Buffer As Byte() = UTF8.GetBytes(Text)
        Return Convert.ToBase64String(DES.CreateEncryptor().TransformFinalBlock(Buffer, 0, Buffer.Length))
    End Function

    Public Shared Function Decrypt(ByVal Text As String, ByVal Password As String) As String
        If Text = "" Or Trim(Text) = NR Then Return ""
        DES.Key = MD5Hash(Password)
        DES.Mode = System.Security.Cryptography.CipherMode.ECB

        Try
            Dim Buffer As Byte() = Convert.FromBase64String(Text)
            Dim UTF8 As New System.Text.UTF8Encoding
            Return UTF8.GetString(DES.CreateDecryptor().TransformFinalBlock(Buffer, 0, Buffer.Length))
        Catch
            Return ""
        End Try
    End Function
#End Region

End Class

#End Region

#Region " Hyperlink "
Class myLink
    Private Shared Function CreateConnection(link As String, Optional timeout As Integer = 500) As Net.HttpWebResponse
        'aktivace protokolu Tls 1.2, který je jmenovitě implementován až od Framework 4.5
        Try
            Net.ServicePointManager.SecurityProtocol = CType(3072, Net.SecurityProtocolType)
            Dim wRequest As Net.HttpWebRequest = CType(Net.WebRequest.Create(link), Net.HttpWebRequest)
            wRequest.UserAgent = "pyramidak " + Application.ExeName
            wRequest.Timeout = timeout
            Dim wResponse As Net.HttpWebResponse = CType(wRequest.GetResponse(), Net.HttpWebResponse)
            Return wResponse
        Catch
            Return Nothing
        End Try
    End Function

    Public Shared Sub Start(wnd As Window, link As String)
        If My.Computer.Network.IsAvailable = False Then
            Dim wDialog = New wpfDialog(wnd, "No internet connection.", "open link", wpfDialog.Ikona.chyba, "Zavřít")
            wDialog.ShowDialog()
            Exit Sub
        End If
        Try
            Process.Start(link)
        Catch ex As Exception
            Dim wDialog = New wpfDialog(wnd, ex.Message, "open link", wpfDialog.Ikona.chyba, "Zavřít")
            wDialog.ShowDialog()
        End Try
    End Sub

    Public Shared Sub StartHidden(link As String)
        Try
            If Not link.ToLower.StartsWith("http") Then link = "http://" & link
            Dim wResponse = CreateConnection(link, 1000)
            If wResponse IsNot Nothing Then wResponse.Close()
        Catch
        End Try
    End Sub

    Public Shared Function AbsoluteUri(link As String) As String
        If link Is Nothing OrElse link = "" Then Return ""
        If My.Computer.Network.IsAvailable = False Then Return ""

        If Not link.ToLower.StartsWith("http") Then link = "http://" & link
        AbsoluteUri = ""
        Dim wResponse = CreateConnection(link, 250)
        If wResponse IsNot Nothing Then
            AbsoluteUri = wResponse.ResponseUri.AbsoluteUri().ToString
            wResponse.Close()
        End If
    End Function

    Public Shared Function Exist(link As String) As Boolean
        If link Is Nothing OrElse link = "" Then Return False
        If My.Computer.Network.IsAvailable = False Then Return False

        If Not link.ToLower.StartsWith("http") Then link = "http://" & link
        Exist = False
        Dim wResponse = CreateConnection(link, 250)
        If wResponse IsNot Nothing Then
            Exist = True
            wResponse.Close()
        End If
    End Function

    Public Shared Function Address(link As String) As Boolean
        If link Is Nothing OrElse link = "" Then Return False
        If link.ToLower.StartsWith("https://") Or link.ToLower.StartsWith("http://") Or link.ToLower.StartsWith("www.") Then Return True
        Return False
    End Function

    Public Shared Function WebIcon(link As String) As System.Drawing.Icon
        If link Is Nothing OrElse link = "" Then Return Nothing
        If My.Computer.Network.IsAvailable = False Then Return Nothing

        If Not link.ToLower.StartsWith("http") Then link = "https://" & link
        Dim url As Uri = New Uri(link)
        If url.HostNameType = UriHostNameType.Dns Then
            Dim iconURL = If(link.StartsWith("https"), "https://", "http://") & url.Host & "/favicon.ico"
            Try
                Dim wResponse = CreateConnection(iconURL, 250)
                If wResponse IsNot Nothing Then
                    Dim stream As IO.Stream = wResponse.GetResponseStream()
                    Dim favicon As System.Drawing.Image = System.Drawing.Image.FromStream(stream)
                    wResponse.Close()
                    Dim iconBitmap As System.Drawing.Bitmap = New System.Drawing.Bitmap(favicon)
                    Return System.Drawing.Icon.FromHandle(iconBitmap.GetHicon)
                End If
            Catch
            End Try
        End If

        Return Nothing
    End Function

    Public Shared Function WebName(link As String) As String
        If link Is Nothing OrElse link = "" Then Return ""

        If My.Computer.Network.IsAvailable Then
            If Not link.ToLower.StartsWith("http") Then link = "https://" & link
            Dim url As Uri = New Uri(link)
            If url.HostNameType = UriHostNameType.Dns Then link = url.Host
        End If

        Dim a As Integer = link.IndexOf(".")
        If a = -1 Then
            WebName = link
        Else
            Dim b As Integer = link.IndexOf(".", a + 1)
            If b = -1 Then
                WebName = link.Substring(0, a)
            Else
                WebName = link.Substring(a + 1, b - a - 1)
            End If
        End If
        Return UCase(WebName.Substring(0, 1)) & WebName.Substring(1, WebName.Length - 1)
    End Function

    Private Shared Function RemoveLastSlash(ByVal Link As String) As String
        If Link Is Nothing OrElse Link = "" Then Return ""
        If Link.EndsWith("/") Then
            Return Link.Substring(0, Link.Length - 1)
        Else
            Return Link
        End If
    End Function

End Class

#End Region

#Region " Files "

Class myFile

#Region " Compress File "

    Public Shared Function Compress(ByVal source As String, ByVal destination As String) As Boolean
        If Delete(destination, False) = False Then Return False

        ' Create the streams and byte arrays needed
        Dim buffer As Byte() = Nothing
        Dim sourceStream As IO.FileStream = Nothing
        Dim destinationStream As IO.FileStream = Nothing
        Dim compressedStream As IO.Compression.GZipStream = Nothing
        Try
            ' Read the bytes from the source file into a byte array
            sourceStream = New IO.FileStream(source, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
            ' Read the source stream values into the buffer
            buffer = New Byte(CInt(sourceStream.Length)) {}
            Dim checkCounter As Integer = sourceStream.Read(buffer, 0, buffer.Length)
            ' Open the FileStream to write to
            destinationStream = New IO.FileStream(destination, IO.FileMode.OpenOrCreate, IO.FileAccess.Write)
            ' Create a compression stream pointing to the destiantion stream
            compressedStream = New IO.Compression.GZipStream(destinationStream, IO.Compression.CompressionMode.Compress, True)
            'Now write the compressed data to the destination file
            compressedStream.Write(buffer, 0, buffer.Length)
        Catch ex As ApplicationException
            Call (New wpfDialog(Nothing, ex.Message, "Komprimace souboru", wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
            Return False
        Finally
            ' Make sure we allways close all streams   
            If Not (sourceStream Is Nothing) Then
                sourceStream.Close()
            End If
            If Not (compressedStream Is Nothing) Then
                compressedStream.Close()
            End If
            If Not (destinationStream Is Nothing) Then
                destinationStream.Close()
            End If
        End Try
        Return True
    End Function
#End Region

#Region " Decompress File or Embedded File "

    Public Shared Function Decompress(ByVal source As String, ByVal destination As String) As Boolean
        If Delete(destination, False) = False Then Return False
        Dim sourceStream As IO.Stream
        If Exist(source) Then
            sourceStream = New IO.FileStream(source, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
        Else
            sourceStream = Reflection.Assembly.GetExecutingAssembly.GetManifestResourceStream("RootSpace." & source & ".zip")
        End If

        Dim Reader As New System.IO.BinaryReader(sourceStream)
        Dim fileByte() As Byte = Reader.ReadBytes(CInt(sourceStream.Length))
        ' Create the streams and byte arrays needed
        Dim memStream As IO.MemoryStream = New IO.MemoryStream(fileByte)
        Dim destinationStream As IO.FileStream = Nothing
        Dim decompressedStream As IO.Compression.GZipStream = Nothing
        Dim quartetBuffer As Byte() = Nothing
        Try
            ' Read in the compressed source stream
            'sourceStream = New FileStream(sourceFile, FileMode.Open)
            ' Create a compression stream pointing to the destiantion stream
            decompressedStream = New IO.Compression.GZipStream(memStream, IO.Compression.CompressionMode.Decompress, True)
            ' Read the footer to determine the length of the destination file
            quartetBuffer = New Byte(4) {}
            Dim position As Integer = CType(memStream.Length, Integer) - 4
            memStream.Position = position
            memStream.Read(quartetBuffer, 0, 4)
            memStream.Position = 0
            Dim checkLength As Integer = BitConverter.ToInt32(quartetBuffer, 0)
            Dim buffer(checkLength + 100) As Byte
            Dim offset As Integer = 0
            Dim total As Integer = 0
            ' Read the compressed data into the buffer
            While True
                Dim bytesRead As Integer = decompressedStream.Read(buffer, offset, 100)
                If bytesRead = 0 Then
                    Exit While
                End If
                offset += bytesRead
                total += bytesRead
            End While
            ' Now write everything to the destination file
            destinationStream = New IO.FileStream(destination, IO.FileMode.Create)
            destinationStream.Write(buffer, 0, total - 1)
            ' and flush everyhting to clean out the buffer
            destinationStream.Flush()
            Decompress = True
        Catch ex As ApplicationException
            Decompress = False
            Call (New wpfDialog(Nothing, "Přístup odmítnut do umístění.", "Dekomprese souboru", wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
        Finally
            ' Make sure we allways close all streams
            If Not (Reader Is Nothing) Then
                Reader.Close()
            End If
            If Not (sourceStream Is Nothing) Then
                sourceStream.Close()
            End If
            If Not (memStream Is Nothing) Then
                memStream.Close()
            End If
            If Not (sourceStream Is Nothing) Then
                sourceStream.Close()
            End If
            If Not (decompressedStream Is Nothing) Then
                decompressedStream.Close()
            End If
            If Not (destinationStream Is Nothing) Then
                destinationStream.Close()
            End If
        End Try
    End Function

#End Region

#Region " Safename "

    Public Shared Function GetNameSafe(Name As String) As String
        Name = Name.Replace("?", "")
        Name = Name.Replace("'", "")
        Name = Name.Replace("*", "")
        Name = Name.Replace("/", "")
        Name = Name.Replace("<", "")
        Name = Name.Replace(">", "")
        Name = Name.Replace(":", "")
        Name = Name.Replace("\", "")
        Name = Name.Replace("'", "")
        Name = Name.Replace(Chr(34), "")
        Return Name
    End Function

    Public Shared Function isNameSafe(name As String) As Boolean
        If name.Contains("?") Or name.Contains("'") Or name.Contains("*") Or name.Contains("/") Or name.Contains("<") Or name.Contains(">") Or name.Contains(":") Or name.Contains("\") Or name.Contains(Chr(34)) Then Return False
        Return True
    End Function

    Public Shared Function isPathSafe(path As String) As Boolean
        If path.Contains("?") Or path.Contains("'") Or path.Contains("*") Or path.Contains("/") Or path.Contains("<") Or path.Contains(">") Then Return False
        Return True
    End Function
    Public Shared Function isSearchSafe(name As String) As Boolean
        If name.Contains("!") Or name.Contains("'") Or name.Contains("/") Or name.Contains("<") Or name.Contains(">") Or name.Contains(":") Or name.Contains("\") Or name.Contains(Chr(34)) Then Return False
        Return True
    End Function

#End Region

#Region " Cleansename "
    Public Shared Function GetCleanseName(Name As String) As String
        Dim a, b As Integer
        a = Name.LastIndexOf("(")
        b = Name.LastIndexOf(")")
        If Not a = -1 And Not b = -1 Then
            Name = Name.Substring(0, a) & Name.Substring(b + 1, Name.Length - b - 1)
        End If
        a = Name.LastIndexOf("_")
        If Not a = -1 Then Name = Name.Substring(0, a)
        Name = Name.Replace("_", " ")
        Name = Name.Replace(".", " ")
        Return Name
    End Function

#End Region

#Region " Join "

    Private Shared Function FixPath(Folder As String) As String
        If Folder.StartsWith("\") Then Folder = Folder.Substring(1, Folder.Length - 1)
        Return Folder
    End Function

    Public Shared Function Join(Folder As String, File As String) As String
        Return System.IO.Path.Combine(FixPath(Folder), FixPath(File))
    End Function

    Public Shared Function Join(Folder1 As String, Folder2 As String, File As String) As String
        Return System.IO.Path.Combine(FixPath(Folder1), FixPath(Folder2), FixPath(File))
    End Function

#End Region

#Region " Atributes "

    Public Shared Function Size(ByVal Path As String) As Long
        Dim info As New System.IO.FileInfo(Path)
        Return info.Length
    End Function

    Public Shared Function DateCreation(ByVal Path As String) As Date
        Dim info As New System.IO.FileInfo(Path)
        Return info.CreationTime
    End Function

    Public Shared Function DateChange(ByVal Path As String) As Date
        Dim info As New System.IO.FileInfo(Path)
        Return info.LastWriteTime
    End Function

    Public Shared Function DateOpen(ByVal Path As String) As Date
        Dim info As New System.IO.FileInfo(Path)
        Return info.LastAccessTime
    End Function

    Public Shared Function DateSetOpen(Path As String, Datum As Date) As Boolean
        DateSetOpen = False
        Dim info As New System.IO.FileInfo(Path)
        If info.Exists Then
            Try
                info.LastAccessTime = Datum
                DateSetOpen = True
            Catch Err As Exception
                Call (New wpfDialog(Nothing, "Soubor " & Path & " je nepřístupný.", "LastAccessTime", wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
            End Try
        End If
    End Function

#End Region

#Region " Embedded Resource "

    Public Shared Function ReadEmbeddedResource(FileName As String) As Byte()
        Dim Stream As System.IO.Stream = System.Reflection.Assembly.GetExecutingAssembly.GetManifestResourceStream("RootSpace." & FileName)
        Dim Reader As New System.IO.BinaryReader(Stream)
        Return Reader.ReadBytes(CInt(Stream.Length))
    End Function

#End Region

    Public Shared Function ToStream(cesta As String) As IO.MemoryStream
        Dim ms As New IO.MemoryStream
        Try
            Using stream As New IO.FileStream(cesta, IO.FileMode.Open, IO.FileAccess.Read)
                stream.CopyTo(ms)
            End Using
        Catch
        End Try
        ms.Position = 0
        Return ms
    End Function

    Public Shared Function DiskType(cesta As String) As Integer
        Return New System.IO.DriveInfo(cesta.Substring(0, 1)).DriveType
    End Function

    Public Shared Function Extension(cesta As String) As String
        If cesta = "" Then Return ""
        cesta = RemoveQuotationMarks(cesta)
        Return Strings.Right(cesta, cesta.Length - cesta.LastIndexOf("."))
    End Function

    Public Shared Function Name(cesta As String, Optional spriponou As Boolean = True) As String
        If cesta = "" Then Return ""
        cesta = RemoveQuotationMarks(cesta)
        Dim Fols() As String = Split(cesta, "\".ToCharArray)
        Dim dName As String = Fols(UBound(Fols))
        If spriponou = False Then
            If Not dName.LastIndexOf(".".ToCharArray) = -1 Then
                dName = Strings.Left(dName, dName.LastIndexOf(".".ToCharArray))
            End If
        End If
        Return dName
    End Function

    Public Shared Function Path(cesta As String) As String
        If cesta = "" Then Return ""
        cesta = RemoveQuotationMarks(cesta)
        If cesta.LastIndexOf("\") = -1 Then Return ""
        Return Strings.Left(cesta, cesta.LastIndexOf("\"))
    End Function

    Public Shared Function Arguments(cesta As String) As String
        Dim Pos As Integer = cesta.IndexOf(Chr(34))
        If Not Pos = -1 Then cesta = cesta.Substring(Pos + 1, cesta.Length - Pos - 1)
        Pos = cesta.IndexOf(Chr(34))
        If Not Pos = -1 Then
            Arguments = cesta.Substring(Pos + 1, cesta.Length - Pos - 1)
            If Len(Arguments) > 0 Then Arguments = Arguments.Substring(1, Arguments.Length - 1)
            Return Arguments
        End If
        Pos = cesta.IndexOf("/")
        If Not Pos = -1 Then
            Return cesta.Substring(Pos, cesta.Length - Pos)
        End If
        Return ""
    End Function

    Public Shared Function RemoveQuotationMarks(Text As String) As String
        Dim Pos As Integer = Text.IndexOf(Chr(34))
        If Not Pos = -1 Then Text = Text.Substring(Pos + 1, Len(Text) - Pos - 1)
        Pos = Text.IndexOf(Chr(34))
        If Not Pos = -1 Then Text = Text.Substring(0, Pos)
        Pos = Text.IndexOf("//")
        If Pos = -1 Then
            Pos = Text.IndexOf("/")
            If Not Pos = -1 Then Text = Text.Substring(0, Pos - 1)
        End If
        Return Text
    End Function

    Public Shared Function Exist(cesta As String, Optional SystemFile As Boolean = False) As Boolean
        If cesta = "" Then Return False
        cesta = RemoveQuotationMarks(cesta)
        Try
            Dim exFile As New System.IO.FileInfo(cesta)
            If exFile.Exists = False Then Return False
            If SystemFile = False AndAlso exFile.Attributes = 6 Or exFile.Attributes = 38 Then Return False
        Catch
            Return False
        End Try
        Return True
    End Function

    Public Shared Function Delete(ByVal Cesta As String, ByVal Kos As Boolean,
                             Optional ByVal Informovat As Boolean = True,
                             Optional ByVal DelReadOnly As Boolean = False) As Boolean
        If Cesta = "" Then Return False
        Cesta = RemoveQuotationMarks(Cesta)
        Dim sTitle As String = "Mazání souboru "
        Try
            Dim delFile As New System.IO.FileInfo(Cesta)
            If delFile.Exists = False Then Return True
            If delFile.IsReadOnly Then
                If DelReadOnly Then
                    delFile.Attributes = IO.FileAttributes.Normal
                Else
                    If Informovat Then Call (New wpfDialog(Nothing, "Soubor " & Cesta & " je nepřístupný.", sTitle, wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
                    Return False
                End If
            End If
            If Kos Then
                My.Computer.FileSystem.DeleteFile(Cesta, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Else
                delFile.Delete()
            End If
        Catch
            If Informovat Then Call (New wpfDialog(Nothing, "Soubor " & Cesta & " je nepřístupný.", sTitle, wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
            Return False
        End Try
        Return True
    End Function

    Public Shared Function Copy(source As String, destination As String, Optional overwrite As Boolean = True) As Boolean
        If Exist(source, True) Then
            If myFolder.Exist(Path(destination), True) Then
                If overwrite = False Then
                    Dim a As Integer
                    Dim newFile As String = destination
                    Do Until Not myFile.Exist(newFile)
                        a += 1
                        newFile = destination & "(" & CStr(a) & ")"
                    Loop
                    destination = newFile
                End If
                If Delete(destination, False) Then
                    Try
                        FileCopy(source, destination)
                        Return True
                    Catch ex As Exception
                    End Try
                End If
            End If
        End If
        Return False
    End Function

    Public Shared Function Move(ByVal source As String, ByVal destination As String) As Boolean
        If Copy(source, destination) Then
            Delete(source, False)
            Return True
        End If
        Return False
    End Function

    Public Shared Sub Launch(Wnd As Window, ByVal cesta As String, Optional ByVal admin As Boolean = False, Optional ErrMsg As String = "")
        Dim newProcess As New System.Diagnostics.ProcessStartInfo()
        newProcess.FileName = RemoveQuotationMarks(cesta)
        newProcess.Arguments = Arguments(cesta)
        newProcess.WorkingDirectory = Path(newProcess.FileName)
        If admin Then newProcess.Verb = "runas"
        newProcess.CreateNoWindow = True
        Try
            Process.Start(newProcess)
        Catch Ex As Exception
            If Not Err.Number = 5 Then Call (New wpfDialog(Wnd, cesta + NR + NR + If(ErrMsg = "", Ex.Message, ErrMsg), "Otevření selhalo", wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
        End Try
    End Sub

End Class

#End Region

#Region " Folders "

Class myFolder

#Region " Get Files and Folders "

    Public Shared Function SubFolders(Slozka As String, SystemFolder As Boolean) As String()
        Dim Dir As New IO.DirectoryInfo(Slozka)
        If Dir.GetDirectories.Length = 0 Then
            Return Nothing
        Else
            Return Dir.GetDirectories.Select(Function(x) x.FullName).Where(Function(y) Exist(y, False, SystemFolder) = True).ToArray
        End If
    End Function

    Private Shared Sub LoadSubFolders(Slozka As String)
        FindFiles(Slozka)
        Dim Slozky As String() = SubFolders(Slozka, bSysFileDir)
        If bDoSubFolders = False Or Slozky Is Nothing Then Exit Sub
        For Each oneSlozka As String In Slozky
            LoadSubFolders(oneSlozka)
        Next
    End Sub

    Private Shared FoundFiles As New List(Of String)
    Private Shared sPatern As String
    Private Shared bDoSubFolders, bSysFileDir As Boolean

    Public Shared Function Files(Slozka As String, Optional Patern As String = "*.*", Optional DoSubFolders As Boolean = False, Optional SysFileDir As Boolean = False) As String()
        FoundFiles.Clear() : sPatern = Patern : bDoSubFolders = DoSubFolders : bSysFileDir = SysFileDir
        If Exist(Slozka, False, False) = False Then Return FoundFiles.ToArray
        LoadSubFolders(Slozka)
        Return FoundFiles.ToArray
    End Function

    Private Shared Sub FindFiles(ByVal Slozka As String)
        Dim allFiles() As String
        Dim Filters() As String = Split(sPatern, ";")
        For Each oneFilter In Filters
            allFiles = IO.Directory.GetFiles(Slozka, oneFilter)
            For Each File In allFiles
                If myFile.Exist(File, bSysFileDir) Then
                    FoundFiles.Add(File)
                End If
            Next
        Next
    End Sub

#End Region

#Region " Volume serial number"

    Public Shared Function VolumeSerialNumber(ByVal cesta As String) As String
        If cesta = "" Then Return ""
        Try
            Dim mo As New System.Management.ManagementObject("Win32_LogicalDisk.DeviceID=""" & cesta.Substring(0, 1) & ":" & """")
            Dim pd As System.Management.PropertyData = mo.Properties("VolumeSerialNumber")
            Return pd.Value.ToString()
        Catch
        End Try
        Return ""
    End Function

#End Region

#Region " Delete Empty Folders "

    Public Shared Function DeleteEmpty(ByVal Folder As String, Optional ByVal Subfolders As Boolean = False) As Boolean
        If Exist(Folder) = False Then Return False
        deleteEmptyDirs(Folder, Subfolders)
        Return True
    End Function

    Private Shared Sub deleteEmptyDirs(ByVal Folder As String, ByVal Subfolders As Boolean)
        If Subfolders Then
            For Each oneDir As String In System.IO.Directory.GetDirectories(Folder)
                If Exist(Folder) Then
                    deleteEmptyDirs(oneDir, Subfolders)
                End If
            Next
        End If
        Try
            If IO.Directory.GetDirectories(Folder).GetLength(0) = 0 And IO.Directory.GetFiles(Folder).GetLength(0) = 0 Then IO.Directory.Delete(Folder)
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region " Join "

    Private Shared Function FixPath(Folder As String) As String
        If Folder.StartsWith("\") Then Folder = Folder.Substring(1, Folder.Length - 1)
        Return Folder
    End Function

    Public Shared Function Join(Folder1 As String, Folder2 As String) As String
        Return System.IO.Path.Combine(FixPath(Folder1), FixPath(Folder2))
    End Function

    Public Shared Function Join(Folder1 As String, Folder2 As String, Folder3 As String) As String
        Return System.IO.Path.Combine(FixPath(Folder1), FixPath(Folder2), FixPath(Folder3))
    End Function

    Public Shared Function Join(Folder1 As String, Folder2 As String, Folder3 As String, Folder4 As String) As String
        Return System.IO.Path.Combine(FixPath(Folder1), FixPath(Folder2), FixPath(Folder3), FixPath(Folder4))
    End Function

#End Region

    Public Shared Function CheckAccess(cesta As String) As Boolean
        CheckAccess = False
        Dim sFile As String = Join(cesta, "access.test")
        If IO.Directory.Exists(cesta) Then
            Try
                Using fs As New IO.FileStream(sFile, IO.FileMode.CreateNew, IO.FileAccess.Write)
                    fs.WriteByte(&HFF)
                End Using
                If IO.File.Exists(sFile) Then
                    IO.File.Delete(sFile)
                    CheckAccess = True
                End If
            Catch generatedExceptionName As Exception
            End Try
        End If
    End Function

    Public Shared Function DiskType(ByVal cesta As String) As Integer
        Return New System.IO.DriveInfo(cesta.Substring(0, 1)).DriveType
    End Function

    Public Shared Function Name(ByVal cesta As String) As String
        If cesta = "" Then Return ""
        cesta = RemoveQuotationMarks(cesta)
        If cesta.Length < 4 Then Return cesta
        Dim Fols() As String = Split(cesta, "\".ToCharArray)
        Dim dName As String = Fols(UBound(Fols))
        Return dName
    End Function

    Public Shared Function Path(ByVal cesta As String) As String
        If cesta = "" Then Return ""
        cesta = RemoveQuotationMarks(cesta)
        If cesta.LastIndexOf("\") = -1 Or cesta.LastIndexOf("\") = 2 Then Return cesta
        cesta = cesta.Substring(0, cesta.LastIndexOf("\"))
        If cesta.Length = 2 Then cesta += "\"
        Return cesta
    End Function

    Private Shared Function RemoveQuotationMarks(ByVal Text As String) As String
        If Text = "" Then Return Text
        Dim Pos As Integer = Text.IndexOf(Chr(34))
        If Not Pos = -1 Then Text = Text.Substring(Pos + 1, Len(Text) - Pos - 1)
        Pos = Text.IndexOf(Chr(34))
        If Not Pos = -1 Then Text = Text.Substring(0, Pos)
        Pos = Text.IndexOf("//")
        If Pos = -1 Then
            Pos = Text.IndexOf("/")
            If Not Pos = -1 Then Text = Text.Substring(0, Pos - 1)
        End If
        Return Text
    End Function

    Public Shared Function Exist(ByVal cesta As String, Optional ByVal vytvorit As Boolean = False, Optional AllowSysDirs As Boolean = True) As Boolean
        If cesta = "" Then Return False
        cesta = RemoveQuotationMarks(cesta)
        Dim sName As String = Name(cesta).ToLower
        If sName.StartsWith("found.") Or sName = "perflogs" Or sName = "intel" Or sName = "system volume information" Or sName = "recycler" Or sName.Substring(0, 1) = "$" Or sName = "recycled" Or sName = "onedrivetemp" Or sName = "windows.old" Or sName = "system32" _
             Or sName = "programdata" Or sName = "msocache" Or sName.Substring(0, 1) = "." Or sName = "recovery" Or sName = "boot" Or sName = "appdata" Or sName = "intelgraphicsprofiles" Or sName = "inetpub" Then Return False
        If AllowSysDirs = False Then
            If sName = "users" Or sName = "windows" Or sName = "program files" Or sName = "program files (x86)" Or sName = "documents and settings" Then Return False
        End If
        Try
            Dim checkDir As New System.IO.DirectoryInfo(cesta)
            If checkDir.Exists = False Then
                If vytvorit Then
                    checkDir.Create()
                Else
                    Return False
                End If
            End If
            If checkDir.Root.ToString = checkDir.FullName Then Return True
            If checkDir.Attributes = 18 Or checkDir.Attributes = 19 Or checkDir.Attributes = 22 Then Return False
            checkDir.GetFiles("*.txt", IO.SearchOption.TopDirectoryOnly)
        Catch
            Return False
        End Try
        Return True
    End Function

    Public Shared Function Delete(ByVal Cesta As String, ByVal Kos As Boolean,
                             Optional ByVal Informovat As Boolean = True,
                             Optional ByVal DelReadOnly As Boolean = False) As Boolean
        If Cesta = "" Then Return False
        Cesta = RemoveQuotationMarks(Cesta)
        Dim sTitle As String = "Mazání souboru"
        Try
            Dim delFile As New System.IO.FileInfo(Cesta)
            If delFile.Exists = False Then Return True
            If delFile.IsReadOnly Then
                If DelReadOnly Then
                    delFile.Attributes = IO.FileAttributes.Normal
                Else
                    If Informovat Then Call (New wpfDialog(Nothing, "Soubor " & Cesta & " je nepřístupný.", sTitle, wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
                    Return False
                End If
            End If
            If Kos Then
                My.Computer.FileSystem.DeleteFile(Cesta, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Else
                delFile.Delete()
            End If
        Catch
            If Informovat Then Call (New wpfDialog(Nothing, "Soubor " & Cesta & " je nepřístupný.", sTitle, wpfDialog.Ikona.varovani, "Zavřít")).ShowDialog()
            Return False
        End Try
        Return True
    End Function

    Public Shared Function Create(ByVal cesta As String) As Boolean
        cesta = RemoveQuotationMarks(cesta)
        Return Exist(cesta, True)
    End Function

    Public Shared Function RemoveLastSlash(ByVal cesta As String) As String
        cesta = RemoveQuotationMarks(cesta)
        If cesta.EndsWith("\") Then
            Return cesta.Substring(0, cesta.Length - 1)
        Else
            Return cesta
        End If
    End Function

    Public Shared Function Rename(ByVal source As String, ByVal destination As String, Optional ByVal SourceNotExistReturnTrue As Boolean = False) As Boolean
        source = RemoveLastSlash(source) : destination = RemoveLastSlash(destination)
        If Exist(source) = False Then Return SourceNotExistReturnTrue
        If source = destination Then Return False
        Dim myCopy As New clsCopyFolder(source, destination, True, Nothing)
        myCopy.Synch()
        If Exist(destination) Then
            Delete(source, True, False)
            Return True
        End If
        Return False
    End Function

    Public Shared Sub Copy(ByVal source As String, ByVal destination As String, ByVal AndSubfolders As Boolean, Optional myProgressBar As ProgressBar = Nothing)
        If Exist(source) = False Then Exit Sub
        If myProgressBar IsNot Nothing Then myProgressBar.Visibility = Visibility.Visible
        Dim myCopy As New clsCopyFolder(source, destination, AndSubfolders, myProgressBar)
        myCopy.Asynch()
    End Sub

End Class

#Region " Class CopyFolder "

Class clsCopyFolder

    Private SubFolders, wasError As Boolean
    Private CountFile, CountDir As Integer
    Private OldFolder, NewFolder As String
    Private PB As ProgressBar
    Public WithEvents thread As New System.ComponentModel.BackgroundWorker

    Sub New(ByVal SourceFolder As String, ByVal DestinationFolder As String, ByVal AndSubfolders As Boolean, ByVal myPB As ProgressBar)
        OldFolder = ClearPath(SourceFolder) : NewFolder = ClearPath(DestinationFolder) : SubFolders = AndSubfolders : wasError = False
        CountFile = 0 : CountDir = 0
        countDirs(OldFolder)
        If myPB IsNot Nothing Then
            PB = myPB
            myPB.Minimum = 0
            myPB.Value = 0
            myPB.Maximum = CountFile
        End If
        thread.WorkerReportsProgress = If(myPB Is Nothing, False, True)
    End Sub

    Public Sub Synch()
        If Not OldFolder = NewFolder And myFolder.Exist(OldFolder) Then
            If SubFolders Then
                copyDirs(OldFolder, NewFolder)
            Else
                copyFiles(OldFolder, NewFolder)
            End If
        End If
    End Sub

    Public Sub Asynch()
        thread.RunWorkerAsync()
    End Sub

    Private Sub thread_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles thread.DoWork
        Synch()
    End Sub

    Private Sub thread_ProgressChanged(sender As Object, e As ComponentModel.ProgressChangedEventArgs) Handles thread.ProgressChanged
        If PB.Value + e.ProgressPercentage <= PB.Maximum Then PB.Value += e.ProgressPercentage
    End Sub

    Private Sub thread_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles thread.RunWorkerCompleted
        If thread.WorkerReportsProgress Then PB.Visibility = Visibility.Collapsed
    End Sub

    Private Sub copyDirs(ByVal oldFolder As String, ByVal newFolder As String)
        copyFiles(oldFolder, newFolder)
        For Each oneDir As String In System.IO.Directory.GetDirectories(oldFolder)
            If myFolder.Exist(oldFolder) Then
                copyDirs(oneDir, newFolder + "\" + myFolder.Name(oneDir))
            End If
        Next
    End Sub

    Private Sub copyFiles(ByVal oldFolder As String, ByVal newFolder As String)
        Dim bPokazde As Boolean = False
        Dim bKopirovat As Boolean = True
        For Each file As IO.FileInfo In New IO.DirectoryInfo(oldFolder).GetFiles
            If thread.WorkerReportsProgress Then thread.ReportProgress(1)
            If myFile.Exist(newFolder & "\" & file.Name) Then
                bPokazde = True : bKopirovat = False  'vyřazení MessageDialogu, protože Wpf nesmí z asynch spustit další okno
                If bPokazde = False Then
                    Dim FormDialog = New wpfDialog(Nothing, "Soubor již existuje. Chcete ho nahradit?" & NR & newFolder & "\" & file.Name, Application.ProductName & " " & Application.Version, wpfDialog.Ikona.dotaz, "Nahradit", "Přeskočit", False, True, "Použít pro všechny soubory")
                    bKopirovat = CBool(FormDialog.ShowDialog)
                    If FormDialog.Zatrzeno Then bPokazde = True
                End If
                If bKopirovat Then
                    myFile.Delete(newFolder & "\" & file.Name, False, True)
                    Try
                        myFile.Copy(oldFolder & "\" & file.Name, newFolder & "\" & file.Name)
                    Catch
                        wasError = True
                    End Try
                End If
            Else
                Try
                    myFile.Copy(oldFolder & "\" & file.Name, newFolder & "\" & file.Name)
                Catch
                    wasError = True
                End Try
            End If
        Next
    End Sub

    Private Function ClearPath(ByVal FullNamePath As String) As String
        If FullNamePath.EndsWith("\") Then
            Return FullNamePath.Substring(0, FullNamePath.Length - 1)
        Else
            Return FullNamePath
        End If
    End Function

    Private Sub countDirs(ByVal oldFolder As String)
        countFiles(oldFolder)
        CountDir += System.IO.Directory.GetDirectories(oldFolder).Length
        For Each oneDir As String In System.IO.Directory.GetDirectories(oldFolder)
            countDirs(oneDir)
        Next
    End Sub
    Private Sub countFiles(ByVal oldFolder As String)
        CountFile += New IO.DirectoryInfo(oldFolder).GetFiles.Length
    End Sub
End Class

#End Region

#End Region

#Region " Window "

Class myWindow

#Region " PPI Screen Conversion "

    Public Shared Function PPItoPixel(PPI As Point, Always As Boolean) As Point
        If Application.Current.MainWindow Is Nothing Then Return New Point(0, 0)
        Dim transform As Matrix = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice
        If Always Then
            Return transform.Transform(PPI)
        Else
            Dim Height As Double = SystemParameters.PrimaryScreenHeight
            If Height = 720 Or Height = 768 Or Height = 800 Or Height = 900 Or Height = 1024 Or Height = 1050 Or Height = 1080 Or Height = 1200 Or Height = 1440 Or Height = 1600 Or Height = 2160 Or Height = 4320 Then
                Return PPI
            Else
                Return transform.Transform(PPI)
            End If
        End If
    End Function

    Public Shared Function PixelToPPI(PIX As Point, Always As Boolean) As Point
        Dim transform As Matrix = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformFromDevice
        If Always Then
            Return transform.Transform(PIX)
        Else
            Dim Height As Double = SystemParameters.PrimaryScreenHeight
            If Height = 720 Or Height = 768 Or Height = 800 Or Height = 900 Or Height = 1024 Or Height = 1050 Or Height = 1080 Or Height = 1200 Or Height = 1440 Or Height = 1600 Or Height = 2160 Or Height = 4320 Then
                Return transform.Transform(PIX)
            Else
                Return PIX
            End If
        End If
    End Function

#End Region

#Region " Mouse Position "

    <DllImport("user32.dll")>
    Private Shared Function GetCursorPos(ByRef pt As Win32Point) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure Win32Point
        Public X As Int32
        Public Y As Int32
    End Structure

    Public Shared Function GetMousePosition() As System.Windows.Point
        Dim w32Mouse As New Win32Point()
        GetCursorPos(w32Mouse)
        Return New System.Windows.Point(w32Mouse.X, w32Mouse.Y)
    End Function

#End Region

#Region " Do Events "

    Public Shared Sub DoEvents()
        WaitForPriority(System.Windows.Threading.DispatcherPriority.Background)
    End Sub

    Private Shared Sub WaitForPriority(ByVal priority As System.Windows.Threading.DispatcherPriority)
        Dim frame As New System.Windows.Threading.DispatcherFrame()
        Dim dispatcherOperation As System.Windows.Threading.DispatcherOperation = System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(priority, New System.Windows.Threading.DispatcherOperationCallback(AddressOf ExitFrameOperation), frame)
        System.Windows.Threading.Dispatcher.PushFrame(frame)
        If dispatcherOperation.Status <> System.Windows.Threading.DispatcherOperationStatus.Completed Then
            dispatcherOperation.Abort()
        End If
    End Sub

    Private Shared Function ExitFrameOperation(ByVal obj As Object) As Object
        DirectCast(obj, System.Windows.Threading.DispatcherFrame).Continue = False
        Return Nothing
    End Function
#End Region

#Region " Move Form "

    Public Declare Sub ReleaseCapture Lib "User32" ()
    Public Declare Function SendMessage Lib "User32" Alias "SendMessageA" (ByVal hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByRef lParam As Int32) As Integer

    Public Shared Sub Drag(ByVal wnd As Window)
        Dim mainWindowPtr As IntPtr = New System.Windows.Interop.WindowInteropHelper(wnd).Handle
        Dim mainWindowSrc As System.Windows.Interop.HwndSource = System.Windows.Interop.HwndSource.FromHwnd(mainWindowPtr)
        SendMessage(CInt(mainWindowSrc.Handle), &HA1S, 2, 0) '&HA1S=WM_NCLBUTTONDOWN; 2=HTCAPTION
    End Sub
#End Region

End Class

#End Region

#Region " Converter Of Colors "

Class myColorConverter

#Region " Light or Dark Color "

    Public Shared Function LighterColor(inColor As Color, dFactor As Double) As System.Windows.Media.Color
        inColor.R = CByte(inColor.R + (255 - inColor.R) * dFactor)
        inColor.G = CByte(inColor.G + (255 - inColor.G) * dFactor)
        inColor.B = CByte(inColor.B + (255 - inColor.B) * dFactor)
        Return inColor
    End Function

    Public Shared Function DarkerColor(inColor As Color, dFactor As Double) As System.Windows.Media.Color
        inColor.R = CByte(inColor.R * (1 - dFactor))
        inColor.G = CByte(inColor.G * (1 - dFactor))
        inColor.B = CByte(inColor.B * (1 - dFactor))
        Return inColor
    End Function

#End Region

#Region " Drawing.Color and Media.Color "

    Public Shared Function ColorDrawingToMedia(dColor As System.Drawing.Color) As System.Windows.Media.Color
        Return System.Windows.Media.Color.FromArgb(dColor.A, dColor.R, dColor.G, dColor.B)
    End Function

    Public Shared Function ColorMediaToDrawing(mColor As System.Drawing.Color) As System.Drawing.Color
        Return System.Drawing.Color.FromArgb(mColor.A, mColor.R, mColor.G, mColor.B)
    End Function
#End Region

#Region " Brush and String "

    Public Shared Function BrushToColor(ByVal Brush As System.Windows.Media.Brush) As System.Windows.Media.Color
        Dim SCB As SolidColorBrush = DirectCast(Brush, SolidColorBrush)
        Return SCB.Color
    End Function

    Public Shared Function ColorToBrush(ByVal mColor As System.Windows.Media.Color) As System.Windows.Media.Brush
        Return New SolidColorBrush(mColor)
    End Function

    Public Shared Function BrushToString(ByVal Brush As System.Windows.Media.Brush) As String
        Return BrushToColor(Brush).ToString
    End Function

    Public Shared Function StringToBrush(ByVal sValue As String) As SolidColorBrush
        Try
            Dim mColor As System.Windows.Media.Color = CType(System.Windows.Media.ColorConverter.ConvertFromString(sValue), System.Windows.Media.Color)
            Return New SolidColorBrush(mColor)
        Catch ex As Exception
            Return New SolidColorBrush(Colors.Red)
        End Try
    End Function

#End Region

#Region " Convert Media.Colour to Integer "

    Public Shared Function ColorToInt(ByVal mColor As System.Windows.Media.Color) As Integer
        Dim dColor As System.Drawing.Color = System.Drawing.Color.FromArgb(mColor.A, mColor.R, mColor.G, mColor.B)
        Return System.Drawing.ColorTranslator.ToOle(dColor)
    End Function

    Public Shared Function BrushToInt(ByVal Brush As System.Windows.Media.Brush) As Integer
        Dim SCB As SolidColorBrush = DirectCast(Brush, SolidColorBrush)
        Return ColorToInt(SCB.Color)
    End Function

#End Region

#Region " Convert Integer to Media.Colour to String "

    Public Shared Function IntToColor(ByVal iColor As Integer) As System.Windows.Media.Color
        Dim winFormsColor As System.Drawing.Color = System.Drawing.ColorTranslator.FromOle(iColor)
        Return System.Windows.Media.Color.FromArgb(255, winFormsColor.B, winFormsColor.G, winFormsColor.R)
    End Function

    Public Shared Function IntToBrush(ByVal iColor As Integer) As SolidColorBrush
        Return New SolidColorBrush(IntToColor(iColor))
    End Function

    Public Shared Function IntToString(ByVal iColor As Integer) As String
        Return IntToColor(iColor).ToString
    End Function
#End Region

#Region " Get Color Name "

    Public Shared Function ColorToName(ByVal mColor As System.Windows.Media.Color) As String
        Dim clrKnownColor As System.Windows.Media.Color

        'Use reflection to get all known colors
        Dim ColorType As Type = GetType(System.Windows.Media.Colors)
        Dim arrPiColors As System.Reflection.PropertyInfo() = ColorType.GetProperties(System.Reflection.BindingFlags.[Public] Or System.Reflection.BindingFlags.[Static])

        'Iterate over all known colors, convert each to a <Color> and then compare
        'that color to the passed color.
        For Each pi As System.Reflection.PropertyInfo In arrPiColors
            clrKnownColor = DirectCast(pi.GetValue(Nothing, Nothing), System.Windows.Media.Color)
            If clrKnownColor = mColor Then
                Return pi.Name
            End If
        Next

        Return String.Empty
    End Function

    Public Shared Function NameToColor(ByVal sName As String) As System.Windows.Media.Color
        Dim mColor As System.Windows.Media.Color = Nothing
        Try
            Dim objValue As Object = System.Windows.Media.ColorConverter.ConvertFromString(sName)
            If (objValue IsNot Nothing) Then mColor = DirectCast(objValue, System.Windows.Media.Color)
            Return mColor
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function GetKnownColors() As List(Of KeyValuePair(Of String, System.Windows.Media.Color))
        Dim lst As New List(Of KeyValuePair(Of String, System.Windows.Media.Color))()
        Dim ColorType As Type = GetType(System.Windows.Media.Colors)
        Dim arrPiColors As System.Reflection.PropertyInfo() = ColorType.GetProperties(System.Reflection.BindingFlags.[Public] Or System.Reflection.BindingFlags.[Static])

        For Each pi As System.Reflection.PropertyInfo In arrPiColors
            lst.Add(New KeyValuePair(Of String, System.Windows.Media.Color)(pi.Name, DirectCast(pi.GetValue(Nothing, Nothing), System.Windows.Media.Color)))
        Next
        Return lst
    End Function

#End Region

End Class
#End Region

#Region " Bitmap "

Class myBitmap

#Region " GrayScale "

    Public Shared Function ToGrayScale(ByVal srcImage As ImageSource) As ImageSource
        Dim grayBitmap As New FormatConvertedBitmap()
        grayBitmap.BeginInit()
        grayBitmap.Source = CType(srcImage, BitmapSource)
        grayBitmap.DestinationFormat = PixelFormats.Gray8
        grayBitmap.EndInit()
        Return grayBitmap
    End Function

#End Region

#Region " Bitmap conversion "

    Public NotInheritable Class BitmapConversion
        Private Sub New()
        End Sub

        '<System.Runtime.CompilerServices.Extension()> _
        Public Shared Function ToDrawingBitmap(bitmapsource As BitmapSource) As System.Drawing.Bitmap
            Using stream As New System.IO.MemoryStream()
                Dim enc As BitmapEncoder = New BmpBitmapEncoder()
                enc.Frames.Add(BitmapFrame.Create(bitmapsource))
                enc.Save(stream)

                Using tempBitmap = New System.Drawing.Bitmap(stream)
                    ' According to MSDN, one "must keep the stream open for the lifetime of the Bitmap."
                    ' So we return a copy of the new bitmap, allowing us to dispose both the bitmap and the stream.
                    Return New System.Drawing.Bitmap(tempBitmap)
                End Using
            End Using
        End Function

        '<System.Runtime.CompilerServices.Extension()> _
        Public Shared Function ToBitmapSource(bitmap As System.Drawing.Bitmap) As BitmapSource
            Using stream As New System.IO.MemoryStream()
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp)

                stream.Position = 0
                Dim result As New BitmapImage()
                result.BeginInit()
                ' According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                ' Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad
                result.StreamSource = stream
                result.EndInit()
                result.Freeze()
                Return result
            End Using
        End Function
    End Class

#End Region

#Region " Cursor "
    Public Shared Function ToCursor(ByVal source As DrawingImage, dimension As Size) As Cursor
        'BitmapSource
        Dim drawingVisual As DrawingVisual = New DrawingVisual()
        Dim drawingContext As DrawingContext = drawingVisual.RenderOpen()
        drawingContext.DrawImage(source, New Rect(New Point(0, 0), dimension))
        drawingContext.Close()
        Dim bmp As RenderTargetBitmap = New RenderTargetBitmap(CInt(dimension.Width), CInt(dimension.Height), 96, 96, PixelFormats.Pbgra32)
        bmp.Render(drawingVisual)

        'BitmapImage
        Dim encoder As PngBitmapEncoder = New PngBitmapEncoder()
        Dim memoryStream As IO.MemoryStream = New IO.MemoryStream()
        Dim bImg As BitmapImage = New BitmapImage()
        encoder.Frames.Add(BitmapFrame.Create(bmp))
        encoder.Save(memoryStream)
        memoryStream.Position = 0
        bImg.BeginInit()
        bImg.StreamSource = memoryStream
        bImg.EndInit()
        Return ToCursor(bImg.StreamSource)
        memoryStream.Close()
    End Function

    Public Shared Function ToCursor(ByVal Ico As System.Drawing.Icon) As Cursor
        Dim handle As New SafeIconHandle(Ico.Handle)
        Return System.Windows.Interop.CursorInteropHelper.Create(handle)
    End Function

    Public Shared Function ToCursor(ByVal myURI As Uri) As Cursor
        Dim imgStream As IO.Stream = Application.GetResourceStream(myURI).Stream
        If myURI.ToString.EndsWith("cur") Or myURI.ToString.EndsWith("ico") Then
            Return New Cursor(imgStream)
        Else
            Return ToCursor(imgStream)
        End If
    End Function

    Public Shared Function ToCursor(ByVal IOStream As IO.Stream) As Cursor
        Dim bit As New System.Drawing.Bitmap(IOStream)
        If bit.Size.Width > 64 Then bit = ResizeBitmap(bit, 64, 64)
        Dim curPtr As IntPtr = bit.GetHicon()
        Dim handle As New SafeIconHandle(curPtr)
        Return System.Windows.Interop.CursorInteropHelper.Create(handle)
    End Function

    Public Shared Function ToCursor(ByVal imgBitmap As BitmapImage) As Cursor
        Return ToCursor(imgBitmap.StreamSource)
    End Function

    Private Shared Function ResizeBitmap(bit As System.Drawing.Bitmap, nWidth As Integer, nHeight As Integer) As System.Drawing.Bitmap
        Dim result As New System.Drawing.Bitmap(nWidth, nHeight)
        Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(DirectCast(result, System.Drawing.Image))
            g.DrawImage(bit, 0, 0, nWidth, nHeight)
        End Using
        Return result
    End Function

    Class SafeIconHandle
        Inherits Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
        <DllImport("user32.dll", SetLastError:=True)> _
        Friend Shared Function DestroyIcon(<[In]()> hIcon As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        Private Sub New()
            MyBase.New(True)
        End Sub

        Public Sub New(hIcon As IntPtr)
            MyBase.New(True)
            Me.SetHandle(hIcon)
        End Sub

        Protected Overrides Function ReleaseHandle() As Boolean
            Return DestroyIcon(Me.handle)
        End Function
    End Class

#End Region

#Region " Icon "

    Public Shared Function IconToImageSource(ByVal Ico As System.Drawing.Icon) As ImageSource
        Return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Ico.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
    End Function

    Public Shared Function UriToIcon(ByVal myURI As Uri) As System.Drawing.Icon
        Dim iconStream As System.IO.Stream = Application.GetResourceStream(myURI).Stream
        Return New System.Drawing.Icon(iconStream)
    End Function

    Public Shared Function UriToImageSource(ByVal myURI As Uri) As ImageSource
        Dim iconStream As System.IO.Stream = Application.GetResourceStream(myURI).Stream
        Return IconToImageSource(New System.Drawing.Icon(iconStream))
    End Function

#End Region

#Region " Merge Images "

    'Size 1=same as Image1, 2=half Image1; Place 1=left up, 2=right up
    Public Shared Function Merge(ByVal Image1 As ImageSource, ByVal Image2 As Uri, ByVal iSize As Integer, ByVal iPlace As Integer) As RenderTargetBitmap
        Dim frame2 As BitmapFrame = BitmapDecoder.Create(Image2, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames.First()
        Return Merge(Image1, frame2, iSize, iPlace)
    End Function

    Public Shared Function Merge(ByVal Image1 As ImageSource, ByVal Image2 As ImageSource, ByVal iSize As Integer, ByVal iPlace As Integer) As RenderTargetBitmap
        ' Gets the size of the images (I assume each image has the same size)
        Dim imageWidth As Integer = CInt(Image1.Width)
        Dim imageHeight As Integer = CInt(Image1.Height)

        Dim iLeft, iTop As Double
        Select Case iPlace
            Case 1
                iLeft = 0 : iTop = 0
            Case 2
                iLeft = imageWidth / iSize * 2 : iTop = 0
            Case 3
                iLeft = 0 : iTop = imageHeight / iSize * 2
            Case 4
                iLeft = imageWidth / iSize * 2 : iTop = imageHeight / iSize * 2
        End Select

        ' Draws the images into a DrawingVisual component
        Dim drawingVisual As New DrawingVisual()
        Using drawingContext As DrawingContext = drawingVisual.RenderOpen()
            drawingContext.DrawImage(Image1, New Rect(0, 0, imageWidth, imageHeight))
            drawingContext.DrawImage(Image2, New Rect(iLeft, iTop, imageWidth / iSize, imageHeight / iSize))
        End Using

        ' Converts the Visual (DrawingVisual) into a BitmapSource
        Dim bmp As New RenderTargetBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Pbgra32)
        bmp.Render(drawingVisual)
        Return bmp

        ' Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
        'Dim encoder As New PngBitmapEncoder()
        'encoder.Frames.Add(BitmapFrame.Create(bmp))

        ' Saves the image into a file using the encoder
        'Using stream As IO.Stream = IO.File.Create(pathTileImage)
        ' encoder.Save(stream)
        'End Using
    End Function

#End Region

End Class

#End Region

#Region " INI "

Class myINI

    Public Shared Function GetSetting(ByVal fileINI As String, ByVal Category As String, ByVal Key As String, ByVal Vychozi As String) As String
        Dim sValue As String = New String(Chr(0), 255)
        Dim iSize As Integer = GetPrivateProfileString(Category, Key, Vychozi, sValue, 255, fileINI)
        Return sValue.Substring(0, iSize)
    End Function

    Public Shared Function GetSetting(ByVal fileINI As String, ByVal Category As String, ByVal Key As String, ByVal Vychozi As Integer) As Integer
        Return GetPrivateProfileInt(Category, Key, Vychozi, fileINI)
    End Function

    Private Declare Function GetPrivateProfileString Lib "kernel32" Alias "GetPrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpDefault As String, ByVal lpReturnedString As String, ByVal nSize As Integer, ByVal lpFileName As String) As Integer
    Private Declare Function WritePrivateProfileString Lib "kernel32" Alias "WritePrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpString As String, ByVal lpFileName As String) As Integer
    Private Declare Function GetPrivateProfileInt Lib "kernel32" Alias "GetPrivateProfileIntA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal nDefault As Integer, ByVal lpFileName As String) As Integer

End Class

#End Region

