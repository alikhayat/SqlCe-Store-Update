Imports System.Data.SqlServerCe
Class MergeSync
    Private replobj As SqlCeReplication
    Private Configs As Databases
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
        replobj.SubscriberConnectionString = String.Format("Data Source=|DataDirectory|\{0}.sdf", Configs.DBName)

        Return SyncNow()
    End Function
    Private Function SyncNow() As Boolean
        Try
            Console.WriteLine("Synchronizing Database " + Configs.DBName)
            replobj.Synchronize()
            Return True
        Catch ex As Exception
            'replobj.ReinitializeSubscription(False)
            Return False
        Finally
            replobj.Dispose()
        End Try
    End Function
End Class
