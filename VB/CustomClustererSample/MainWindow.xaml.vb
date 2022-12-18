Imports System.Windows
Imports DevExpress.Xpf.Map

Namespace CustomClustererSample

    Public Partial Class MainWindow
        Inherits Window

        Public Sub New()
            Me.InitializeComponent()
        End Sub

        Private Sub VectorLayer_DataLoaded(ByVal sender As Object, ByVal e As DataLoadedEventArgs)
            Me.map.ZoomToFitLayerItems()
        End Sub
    End Class
End Namespace
