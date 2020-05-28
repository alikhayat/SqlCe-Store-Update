Imports System.Xml
Imports System.IO
Imports System.Threading
Imports System.ComponentModel
Imports System.IO.Compression
Imports System.Security.Principal
Imports System.Net
Imports System.DirectoryServices.AccountManagement

Public Structure Databases
    Dim DBName As String
    Dim DBPath As String
    Dim WebServerPath As String
    Dim ServerIp As String
    Dim WebAlias As String
    Dim PublisherHost As String
    Dim PublisherDb As String
    Dim PublicationName As String
    Dim PublicationLogin As String
    Dim PublicationPass As String
    Dim PathUsername As String
    Dim PathPass As String
    Dim WebDomain As String
End Structure
Module Module1
    Sub Main()
        Initialize()
    End Sub
    Private Sub Initialize()
        Dim Dbs() As Databases = ReadConfigs()
        If Not IsNothing(Dbs) Then
            For i As Integer = 0 To Dbs.Count - 1
                Dim MergeSync As New MergeSync(Dbs(i))
                OnAfterSync(MergeSync.SyncHandle(), Dbs(i))
                'Dim worker As New System.ComponentModel.BackgroundWorker
                'AddHandler worker.DoWork, AddressOf Sync
                'AddHandler worker.RunWorkerCompleted, AddressOf HandleThreadCompletion
                'worker.RunWorkerAsync(Dbs(i))
            Next i
        Else
            Console.WriteLine("Check your configs")
        End If
    End Sub
    Private Sub OnAfterSync(ByVal Synced As Boolean, ByVal DBInfo As Databases)
        If Synced Then
            Console.WriteLine("Sync succeed")
            Console.WriteLine("Compacting database...")
            If compact(ControlChars.Quote & "Data Source=Databases\" & DBInfo.DBName & ".sdf" & ControlChars.Quote) Then
                Console.WriteLine("Zipping file")
                If ZipFiles(DBInfo.DBName) Then
                    Console.WriteLine("Moving file")
                    If CopyFile(DBInfo.PathUsername, DBInfo.PathPass, DBInfo.WebDomain, AppDomain.CurrentDomain.BaseDirectory & DBInfo.DBName & ".zip", DBInfo.WebServerPath & DBInfo.DBName & ".zip") Then
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory & DBInfo.DBName & ".zip")
                        Console.WriteLine("Done")
                    Else
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory & DBInfo.DBName & ".zip")
                    End If
                End If
            Else
                Console.WriteLine("Compact Failed")
                Console.WriteLine("Ignore Database Compact? Y for Yes,N for No")
                Dim response As String = Console.ReadLine
                If response = "Y" Or response = "y" Then
                    ZipFiles(DBInfo.DBName)
                    Console.WriteLine("Done")
                Else
                    Exit Sub
                End If
            End If
        Else
            Console.WriteLine("Sync Failed")
        End If
    End Sub
    Private Function compact(ByVal ConnectionString As String) As Boolean
        Dim cmdProcess As New Process
        With cmdProcess
            .StartInfo = New ProcessStartInfo("sqlcecmd", "-d " & ConnectionString & " -e compact")
            With .StartInfo
                .CreateNoWindow = False
                .UseShellExecute = False
                .RedirectStandardOutput = True
            End With
            .Start()
            .WaitForExit()
        End With

        Dim ipconfigOutput As String = cmdProcess.StandardOutput.ReadToEnd
        If ipconfigOutput = "Database successfully compacted" & vbCrLf Then
            Return True
        Else
            Return False
        End If
    End Function
    'Private Sub Sync(ByVal Sender As Object, ByVal e As DoWorkEventArgs)
    '    Dim DBInfo As Databases = e.Argument
    '    Dim MergeSync As New MergeSync(DBInfo)
    '    Dim a As Boolean = MergeSync.SyncHandle()
    '    MsgBox(a.ToString)
    'End Sub
    Private Sub HandleThreadCompletion(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
        Dim return_value As Boolean = e.Result
        If return_value = False Then
            MsgBox("true")
        End If
        'Zip the db
        'send to it's destination server
    End Sub
    Private Function ReadConfigs() As Databases()
        Console.WriteLine("Gathering configs")
        If IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Configs.xml") Then
            Dim xmlDoc As New XmlDocument()
            xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "Configs.xml")
            Dim nodes As XmlNode = xmlDoc.FirstChild
            Dim DBCount As Integer = nodes.ChildNodes.Count
            If DBCount > 0 Then
                Dim DbConfigs(DBCount - 1) As Databases
                Dim J As Integer = 0
                For Each ioObject As XmlNode In nodes.ChildNodes
                    Dim root As XmlNode = xmlDoc.SelectSingleNode("Databases/" + ioObject.Name)
                    If root.HasChildNodes Then
                        DbConfigs(J).DBPath = root.ChildNodes(0).InnerText
                        DbConfigs(J).DBName = root.ChildNodes(1).InnerText
                        DbConfigs(J).ServerIp = root.ChildNodes(2).InnerText
                        DbConfigs(J).WebAlias = root.ChildNodes(3).InnerText
                        DbConfigs(J).PublisherHost = root.ChildNodes(4).InnerText
                        DbConfigs(J).PublisherDb = root.ChildNodes(5).InnerText
                        DbConfigs(J).PublicationName = root.ChildNodes(6).InnerText
                        DbConfigs(J).PublicationLogin = root.ChildNodes(7).InnerText
                        DbConfigs(J).PublicationPass = root.ChildNodes(8).InnerText
                        DbConfigs(J).WebServerPath = root.ChildNodes(9).InnerText
                        DbConfigs(J).PathUsername = root.ChildNodes(10).InnerText
                        DbConfigs(J).PathPass = root.ChildNodes(11).InnerText
                        DbConfigs(J).WebDomain = root.ChildNodes(12).InnerText
                    End If
                    J += 1
                Next ioObject

                Console.WriteLine("Configs has been inialized")

                Return DbConfigs
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If
    End Function
    Private Function ZipFiles(ByVal FileName As String) As Boolean
        If File.Exists(AppDomain.CurrentDomain.BaseDirectory() & FileName & ".zip") Then
            File.Delete(AppDomain.CurrentDomain.BaseDirectory() & FileName & ".zip")
        End If
        If Not Directory.Exists(AppDomain.CurrentDomain.BaseDirectory() & "TMP") Then
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory() & "TMP")
        Else
            Dim tmp = New DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory() & "TMP")
            For Each File As FileInfo In tmp.GetFiles
                File.Delete()
            Next
        End If

        File.Copy(AppDomain.CurrentDomain.BaseDirectory() & "Databases\" & FileName & ".sdf", AppDomain.CurrentDomain.BaseDirectory() & "TMP\" & FileName & ".sdf")
        ZipFile.CreateFromDirectory(AppDomain.CurrentDomain.BaseDirectory() & "TMP", AppDomain.CurrentDomain.BaseDirectory() & FileName & ".zip")
        Directory.Delete(AppDomain.CurrentDomain.BaseDirectory() & "TMP", True)

        Return True
    End Function
    Private Function CopyFile(ByVal Username As String, ByVal Password As String, ByVal Domian As String, ByVal FilePath As String, ByVal Destination As String) As Boolean
        Try
            Dim validLogin As Boolean = False

            Using tempcontext As PrincipalContext = New PrincipalContext(ContextType.Domain, Domian, Nothing, ContextOptions.Negotiate)
                Try
                    validLogin = tempcontext.ValidateCredentials(Username, Password, ContextOptions.Negotiate)
                Catch ex As Exception
                    Console.WriteLine(ex.Message)
                End Try
            End Using
            If validLogin Then
                File.Copy(FilePath, Destination.Trim, True)
                Return True
            Else
                Console.WriteLine("Username or Password is incorrect...", "Login Error")
                Return False
            End If
        Catch ex As Exception
            Console.WriteLine(ex.Message)
            Return False
        End Try
    End Function
End Module
