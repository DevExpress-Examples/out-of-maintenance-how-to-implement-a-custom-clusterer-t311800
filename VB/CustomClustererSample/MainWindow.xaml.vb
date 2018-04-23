Imports System.Windows
Imports DevExpress.Xpf.Map

Namespace CustomClustererSample
    Partial Public Class MainWindow
        Inherits Window

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub VectorLayer_DataLoaded(ByVal sender As Object, ByVal e As DataLoadedEventArgs)
            map.ZoomToFitLayerItems()
        End Sub
    End Class
End Namespace
