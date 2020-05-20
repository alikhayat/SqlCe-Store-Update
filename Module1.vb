Imports System.Xml
Imports System.IO
Imports System.Threading
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
                Dim worker As New System.ComponentModel.BackgroundWorker
                AddHandler worker.DoWork, AddressOf MergeSync.SyncHandle
                AddHandler worker.RunWorkerCompleted, AddressOf HandleThreadCompletion
                worker.RunWorkerAsync()
            Next i
        Else
            Console.WriteLine("Check your configs")
        End If
    End Sub
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
End Module
