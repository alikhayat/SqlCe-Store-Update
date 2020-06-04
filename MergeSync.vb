Imports System.Data.SqlServerCe
Imports System.Threading
Class MergeSync
    Private replobj As SqlCeReplication
    Private Configs As Databases
    Public Reinialized As Boolean = False
    Public Sub New(ByVal _Configs As Databases)
        Configs = _Configs
    End Sub
    Public Function SyncHandle() As Boolean
        'Setting replication library parameteres
        replobj = New SqlCeReplication()
        replobj.ConnectTimeout = 5000
        replobj.ConnectionManager = True
        replobj.InternetUrl = "http://" + Configs.ServerIp + "/" + Configs.WebAlias + "/sqlcesa35.dll"
        replobj.Publisher = Configs.PublisherHost
        replobj.PublisherDatabase = Configs.PublisherDb
        replobj.Publication = Configs.PublicationName
        replobj.PublisherSecurityMode = SecurityType.DBAuthentication
        replobj.PublisherLogin = Configs.PublicationLogin
        replobj.PublisherPassword = Configs.PublicationPass
        replobj.Subscriber = "Auto"
        replobj.SubscriberConnectionString = String.Format("Data Source=|DataDirectory|\Databases\{0}.sdf", Configs.DBName)

        Return SyncNow()
    End Function
    Private Function SyncNow() As Boolean
        Try
            Console.WriteLine("Synchronizing Database " + Configs.DBName)
            replobj.Synchronize()
            Return True
        Catch ex As Exception
            Console.WriteLine(ex.GetType.FullName)
            Console.WriteLine("Try and Reinialize databases ? Y for yes,N for No")
            Dim Response As String = Reader.ReadLine(5000)
            If Response = "y" Or Response = "Y" Then
                Return Reinialize()
            ElseIf Response = Nothing Then
                Return Reinialize()
            Else
                Return False
            End If
        Finally
            replobj.Dispose()
        End Try
    End Function
    Private Function Reinialize() As Boolean
        Try
            Console.WriteLine("Rinializing Database " + Configs.DBName)
            replobj.ReinitializeSubscription(False)
            replobj.Synchronize()
            Reinialized = True
            Return True
        Catch ex As Exception
            Console.WriteLine("Reinialization failed")
            Return False
        Finally
            replobj.Dispose()
        End Try
    End Function
    Class Reader
        Private Shared inputThread As Thread
        Private Shared getInput, gotInput As AutoResetEvent
        Private Shared input As String

        Shared Sub New()
            getInput = New AutoResetEvent(False)
            gotInput = New AutoResetEvent(False)
            inputThread = New Thread(AddressOf reader)
            inputThread.IsBackground = True
            inputThread.Start()
        End Sub

        Private Shared Sub reader()
            While True
                getInput.WaitOne()
                input = Console.ReadLine()
                gotInput.[Set]()
            End While
        End Sub

        Public Shared Function ReadLine(Optional ByVal timeOutMillisecs As Integer = Timeout.Infinite) As String
            getInput.[Set]()
            Dim success As Boolean = gotInput.WaitOne(timeOutMillisecs)

            If success Then
                Return input
            Else
                Return Nothing
            End If
        End Function
    End Class

End Class
