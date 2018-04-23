Imports DevExpress.Map
Imports DevExpress.Xpf.Map
Imports System
Imports System.Collections.Generic

Namespace CustomClustererSample
    Friend Class CureClusterer
        Inherits MapClustererBase

        Private currentItems As MapVectorItemCollection

        Public Overrides ReadOnly Property Items() As MapVectorItemCollection
            Get
                Return currentItems
            End Get
        End Property

        Public Property ClusterCount() As Integer

        Public Overrides Sub Clusterize(ByVal sourceItems As MapVectorItemCollection, ByVal viewport As MapViewport, ByVal sourceChanged As Boolean)
            If sourceChanged Then
                currentItems = ClusterizeImpl(sourceItems)
                Adapter.OnClustered()
            End If
        End Sub

        Protected Overrides Function CreateObject() As MapDependencyObject
            Return New CureClusterer()
        End Function

        Private Function ClusterizeImpl(ByVal sourceItems As MapVectorItemCollection) As MapVectorItemCollection
            ' Separate localizable and non localizable items.
            Dim nonLocalizableItems As New List(Of MapItem)()
            Dim clusters As New List(Of Cluster)()
            For Each item As MapItem In sourceItems
                Dim localizableItem As ISupportCoordLocation = TryCast(item, ISupportCoordLocation)
                If localizableItem IsNot Nothing Then
                    clusters.Add(Cluster.Initialize(localizableItem))
                Else
                    nonLocalizableItems.Add(item)
                End If
            Next item

            ' Arrange initial clusters in increasing order of distance to a closest cluster.
            clusters = Arrange(clusters)

            ' Aggregate localizable items.
            Do While clusters.Count > ClusterCount
                MergeCloserstClusters(clusters)
            Loop

            ' Convert internal cluster helpers to Map items.
            Dim clusterRepresentatives As MapVectorItemCollection = CreateItemsCollection()
            For i As Integer = 0 To clusters.Count - 1

                Dim cluster_Renamed As Cluster = clusters(i)
                Dim representative As New MapDot() With {.Location = New GeoPoint(cluster_Renamed.CenterPoint.Y, cluster_Renamed.CenterPoint.X), .Size = 100}
                For j As Integer = 0 To cluster_Renamed.Items.Count - 1
                    representative.ClusteredItems.Add(TryCast(cluster_Renamed.Items(j), MapItem))
                Next j
                clusterRepresentatives.Add(representative)
            Next i
            For i As Integer = 0 To nonLocalizableItems.Count - 1
                clusterRepresentatives.Add(nonLocalizableItems(i))
            Next i
            Return clusterRepresentatives
        End Function

        Private Shared Function Arrange(ByVal clusters As List(Of Cluster)) As List(Of Cluster)
            Dim arrangedClusters As New List(Of Cluster)()
            For i As Integer = 0 To clusters.Count - 1

                Dim cluster_Renamed As Cluster = clusters(i)
                AssignClosest(cluster_Renamed, clusters)
                ' Inserts depending on distance to closest.
                Insert(arrangedClusters, cluster_Renamed)
            Next i
            Return arrangedClusters
        End Function


        Private Shared Sub AssignClosest(ByVal cluster_Renamed As Cluster, ByVal clusters As List(Of Cluster))
            If clusters.Count < 2 Then
                Throw New ArgumentOutOfRangeException("Clusters count should be larger than 2.")
            End If
            Dim distancableCluster As Cluster = clusters(0)
            If distancableCluster Is cluster_Renamed Then
                distancableCluster = clusters(1)
            End If
            cluster_Renamed.ClosestCluster = distancableCluster

            For i As Integer = 0 To clusters.Count - 1
                distancableCluster = clusters(i)
                If distancableCluster Is cluster_Renamed Then
                    Continue For
                End If
                Dim distance As Double = cluster_Renamed.DistanceTo(distancableCluster)
                If distance < cluster_Renamed.DistanceToClosest Then
                    cluster_Renamed.ClosestCluster = distancableCluster
                End If
            Next i
        End Sub


        Private Shared Sub Insert(ByVal clusters As List(Of Cluster), ByVal cluster_Renamed As Cluster)
            Dim i As Integer = 0
            Do While i < clusters.Count
                If clusters(i).DistanceToClosest > cluster_Renamed.DistanceToClosest Then
                    clusters.Insert(i, cluster_Renamed)
                    Return
                End If
                i += 1
            Loop
            clusters.Add(cluster_Renamed)
        End Sub

        Private Shared Sub MergeCloserstClusters(ByRef clusters As List(Of Cluster))
            If clusters.Count < 2 Then
                Throw New ArgumentOutOfRangeException("Clusters count should be larger than 2.")
            End If
            Dim cluster1 As Cluster = clusters(0)
            Dim cluster2 As Cluster = cluster1.ClosestCluster
            clusters.RemoveAt(0)
            clusters.Remove(cluster2)
            Dim newCluster As Cluster = Cluster.Merge(cluster1, cluster2)
            clusters.Add(newCluster)
            clusters = Arrange(clusters)
        End Sub
    End Class

    Friend Structure MapPoint
        Public Sub New(ByVal x As Double, ByVal y As Double)
            Me.X = x
            Me.Y = y
        End Sub
        Public Property X() As Double
        Public Property Y() As Double
    End Structure

    Friend Class Cluster

        Private centerPoint_Renamed As MapPoint

        Private items_Renamed As List(Of ISupportCoordLocation)

        Private closestCluster_Renamed As Cluster

        Private distanceToClosest_Renamed As Double

        Public Sub New(ByVal items As List(Of ISupportCoordLocation))
            Me.items_Renamed = items
            centerPoint_Renamed = CalculateCenterPoint(items)
        End Sub


        Public Shared Function Initialize(ByVal item As ISupportCoordLocation) As Cluster

            Dim items_Renamed As New List(Of ISupportCoordLocation)()
            items_Renamed.Add(item)
            Return New Cluster(items_Renamed)
        End Function

        Public ReadOnly Property CenterPoint() As MapPoint
            Get
                Return Me.centerPoint_Renamed
            End Get
        End Property
        Public ReadOnly Property Items() As List(Of ISupportCoordLocation)
            Get
                Return Me.items_Renamed
            End Get
        End Property

        Public Property ClosestCluster() As Cluster
            Get
                Return Me.closestCluster_Renamed
            End Get
            Set(ByVal value As Cluster)
                Me.closestCluster_Renamed = value
                distanceToClosest_Renamed = DistanceTo(closestCluster_Renamed)
            End Set
        End Property

        Public ReadOnly Property DistanceToClosest() As Double
            Get
                Return distanceToClosest_Renamed
            End Get
        End Property

        Public Function DistanceTo(ByVal h As Cluster) As Double
            Return Math.Sqrt((h.CenterPoint.X - CenterPoint.X) * (h.CenterPoint.X - CenterPoint.X) + (h.CenterPoint.Y - CenterPoint.Y) * (h.CenterPoint.Y - CenterPoint.Y))
        End Function

        Public Shared Function Merge(ByVal cluster1 As Cluster, ByVal cluster2 As Cluster) As Cluster
            Dim newItems As New List(Of ISupportCoordLocation)(cluster1.Items)
            newItems.AddRange(cluster2.Items)

            Return New Cluster(newItems)
        End Function

        Public Shared Function CalculateCenterPoint(ByVal items As List(Of ISupportCoordLocation)) As MapPoint
            Dim meanX As Double = 0
            Dim meanY As Double = 0
            For Each item As ISupportCoordLocation In items
                meanX += item.Location.GetX()
                meanY += item.Location.GetY()
            Next item
            meanX /= items.Count
            meanY /= items.Count
            Return New MapPoint(meanX, meanY)
        End Function
    End Class
End Namespace
