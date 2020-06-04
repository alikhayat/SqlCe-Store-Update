Imports System.Xml
Imports System.IO
Imports System.Threading
Imports System.ComponentModel
Imports System.IO.Compression
Imports System.Security.Principal
Imports System.Net
Imports System.DirectoryServices.AccountManagement
Imports System.Data.SqlServerCe

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
                OnAfterSync(MergeSync.SyncHandle(), Dbs(i), MergeSync.Reinialized)
                'Dim worker As New System.ComponentModel.BackgroundWorker
                'AddHandler worker.DoWork, AddressOf Sync
                'AddHandler worker.RunWorkerCompleted, AddressOf HandleThreadCompletion
                'worker.RunWorkerAsync(Dbs(i))
            Next i
        Else
            Console.WriteLine("Check your configs")
        End If
    End Sub
    Private Sub OnAfterSync(ByVal Synced As Boolean, ByVal DBInfo As Databases, ByVal Reinialized As Boolean)
        If Synced Then
            If Reinialized Then
                Dim ConnectionString As String = "Data Source=" & AppDomain.CurrentDomain.BaseDirectory & "Databases\" & DBInfo.DBName & ".sdf"
                Dim IndexesDir As String = AppDomain.CurrentDomain.BaseDirectory & "Indexes\" & DBInfo.DBName
                Dim IndexesBuild As New Build_Indexes(ConnectionString, IndexesDir)
                Console.WriteLine("Creating Indexes")
                If Not IndexesBuild.RebuildIndexes Then
                    Console.WriteLine(String.Format("Operation failed for {0}", DBInfo.DBName))
                    Exit Sub
                End If
            End If
            Console.WriteLine("Sync succeed")
            Console.WriteLine("Compacting database...")
            If Compact(ControlChars.Quote & "Data Source=Databases\" & DBInfo.DBName & ".sdf" & ControlChars.Quote) Then
                Console.WriteLine("Zipping file")
                If ZipFiles(DBInfo.DBName) Then
                    Console.WriteLine("Moving file")
                    If CopyFile(DBInfo.PathUsername + "@" + DBInfo.WebDomain, DBInfo.PathPass, DBInfo.WebDomain, AppDomain.CurrentDomain.BaseDirectory & DBInfo.DBName & ".zip", DBInfo.WebServerPath & DBInfo.DBName & ".zip") Then
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory & DBInfo.DBName & ".zip")
                        Console.WriteLine("Done")
                    Else
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory & DBInfo.DBName & ".zip")
                    End If
                End If
            Else
                Console.WriteLine("Compact Failed")
                Console.WriteLine("Ignore Database Compact? Y for Yes,N for No")
                Dim Reader As New Reader
                Dim Response As String = Reader.ReadLine(5000)
                If Response = "Y" Or Response = "y" Or Response = Nothing Then
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
    Private Function Compact(ByVal ConnectionString As String) As Boolean
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

        Dim Output As String = cmdProcess.StandardOutput.ReadToEnd
        If Output = "Database successfully compacted" & vbCrLf Then
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
                File.Copy(FilePath, Destination.Substring(1), True)
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
    Partial Private Class Build_Indexes
        Private ConnectionString As String
        Private IndexesDir As String
        Dim Conn As SqlCeConnection = New SqlCeConnection(ConnectionString)
        Public Sub New(ByVal _ConnectionString As String, ByVal _IndexesDir As String)
            ConnectionString = _ConnectionString
            IndexesDir = _IndexesDir
            Try
                If Conn.State <> System.Data.ConnectionState.Open Then
                    Conn.Open()
                End If
            Catch ex As Exception
                Console.WriteLine(ex.Message)
            End Try
        End Sub
        Public Function RebuildIndexes()
            Dim Dir = New DirectoryInfo(IndexesDir)
            If Dir.Exists Then
                Dim TbIndexes As String() = Directory.GetFiles(IndexesDir, "*.sqlce", SearchOption.AllDirectories)
                If TbIndexes.Length > 0 Then
                    For i As Integer = 0 To TbIndexes.Length - 1
                        Dim IndexFile As String = TbIndexes(i)
                        Dim QueryCount As Integer = File.ReadAllLines(IndexFile).Length
                        For j As Integer = 0 To QueryCount - 1 Step 2
                            Dim Query As String = File.ReadAllLines(IndexFile)(j)
                            If Query <> String.Empty Then
                                If Not ApplyIndexes(Query) Then
                                    Console.WriteLine("Check Index queries")
                                    Return False
                                    Exit For
                                End If
                            End If
                        Next
                    Next
                    Return True
                Else
                    Console.WriteLine("Indexes files not found")
                    Return False
                End If
            Else
                Console.WriteLine("Directory not found")
                Return False
            End If
        End Function
        Private Function ApplyIndexes(ByVal Query As String)
            Dim Cmd As New SqlCeCommand
            Try
                Cmd = New SqlCeCommand(Query, Conn)
                Cmd.ExecuteNonQuery()
                Return True
            Catch ex As Exception
                Return False
            Finally
                Cmd.Dispose()
                Conn.Close()
            End Try
        End Function
    End Class
End Module
