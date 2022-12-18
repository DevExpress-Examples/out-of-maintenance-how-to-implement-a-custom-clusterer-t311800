Imports DevExpress.Map
Imports DevExpress.Xpf.Map
Imports System
Imports System.Collections.Generic

Namespace CustomClustererSample

    Friend Class CureClusterer
        Inherits MapClustererBase

        Private currentItems As MapVectorItemCollection

        Public Overrides ReadOnly Property Items As MapVectorItemCollection
            Get
                Return currentItems
            End Get
        End Property

        Public Property ClusterCount As Integer

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
            Dim nonLocalizableItems As List(Of MapItem) = New List(Of MapItem)()
            Dim clusters As List(Of Cluster) = New List(Of Cluster)()
            For Each item As MapItem In sourceItems
                Dim localizableItem As ISupportCoordLocation = TryCast(item, ISupportCoordLocation)
                If localizableItem IsNot Nothing Then
                    clusters.Add(Cluster.Initialize(localizableItem))
                Else
                    nonLocalizableItems.Add(item)
                End If
            Next

            ' Arrange initial clusters in increasing order of distance to a closest cluster.
            clusters = Arrange(clusters)
            ' Aggregate localizable items.
            While clusters.Count > ClusterCount
                MergeCloserstClusters(clusters)
            End While

            ' Convert internal cluster helpers to Map items.
            Dim clusterRepresentatives As MapVectorItemCollection = CreateItemsCollection()
            Dim i As Integer = 0
            While i < clusters.Count
                Dim cluster As Cluster = clusters(i)
                Dim representative As MapDot = New MapDot() With {.Location = New GeoPoint(cluster.CenterPoint.Y, cluster.CenterPoint.X), .Size = 100}
                Dim j As Integer = 0
                While j < cluster.Items.Count
                    representative.ClusteredItems.Add(TryCast(cluster.Items(j), MapItem))
                    Threading.Interlocked.Increment(j)
                End While

                clusterRepresentatives.Add(representative)
                Threading.Interlocked.Increment(i)
            End While

            Dim i As Integer = 0
            While i < nonLocalizableItems.Count
                clusterRepresentatives.Add(nonLocalizableItems(i))
                Threading.Interlocked.Increment(i)
            End While

            Return clusterRepresentatives
        End Function

        Private Shared Function Arrange(ByVal clusters As List(Of Cluster)) As List(Of Cluster)
            Dim arrangedClusters As List(Of Cluster) = New List(Of Cluster)()
            Dim i As Integer = 0
            While i < clusters.Count
                Dim cluster As Cluster = clusters(i)
                AssignClosest(cluster, clusters)
                ' Inserts depending on distance to closest.
                Insert(arrangedClusters, cluster)
                Threading.Interlocked.Increment(i)
            End While

            Return arrangedClusters
        End Function

        Private Shared Sub AssignClosest(ByVal cluster As Cluster, ByVal clusters As List(Of Cluster))
            If clusters.Count < 2 Then Throw New ArgumentOutOfRangeException("Clusters count should be larger than 2.")
            Dim distancableCluster As Cluster = clusters(0)
            If distancableCluster Is cluster Then distancableCluster = clusters(1)
            cluster.ClosestCluster = distancableCluster
            Dim i As Integer = 0
            While i < clusters.Count
                distancableCluster = clusters(i)
                If distancableCluster Is cluster Then Continue While
                Dim distance As Double = cluster.DistanceTo(distancableCluster)
                If distance < cluster.DistanceToClosest Then cluster.ClosestCluster = distancableCluster
                Threading.Interlocked.Increment(i)
            End While
        End Sub

        Private Shared Sub Insert(ByVal clusters As List(Of Cluster), ByVal cluster As Cluster)
            Dim i As Integer = 0
            While i < clusters.Count
                If clusters(i).DistanceToClosest > cluster.DistanceToClosest Then
                    clusters.Insert(i, cluster)
                    Return
                End If

                Threading.Interlocked.Increment(i)
            End While

            clusters.Add(cluster)
        End Sub

        Private Shared Sub MergeCloserstClusters(ByRef clusters As List(Of Cluster))
            If clusters.Count < 2 Then Throw New ArgumentOutOfRangeException("Clusters count should be larger than 2.")
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

        Public Property X As Double

        Public Property Y As Double
    End Structure

    Friend Class Cluster

        Private centerPointField As MapPoint

        Private itemsField As List(Of ISupportCoordLocation)

        Private closestClusterField As Cluster

        Private distanceToClosestField As Double

        Public Sub New(ByVal items As List(Of ISupportCoordLocation))
            itemsField = items
            centerPointField = CalculateCenterPoint(items)
        End Sub

        Public Shared Function Initialize(ByVal item As ISupportCoordLocation) As Cluster
            Dim items As List(Of ISupportCoordLocation) = New List(Of ISupportCoordLocation)()
            items.Add(item)
            Return New Cluster(items)
        End Function

        Public ReadOnly Property CenterPoint As MapPoint
            Get
                Return centerPointField
            End Get
        End Property

        Public ReadOnly Property Items As List(Of ISupportCoordLocation)
            Get
                Return itemsField
            End Get
        End Property

        Public Property ClosestCluster As Cluster
            Get
                Return closestClusterField
            End Get

            Set(ByVal value As Cluster)
                closestClusterField = value
                distanceToClosestField = DistanceTo(closestClusterField)
            End Set
        End Property

        Public ReadOnly Property DistanceToClosest As Double
            Get
                Return distanceToClosestField
            End Get
        End Property

        Public Function DistanceTo(ByVal h As Cluster) As Double
            Return Math.Sqrt((h.CenterPoint.X - CenterPoint.X) * (h.CenterPoint.X - CenterPoint.X) + (h.CenterPoint.Y - CenterPoint.Y) * (h.CenterPoint.Y - CenterPoint.Y))
        End Function

        Public Shared Function Merge(ByVal cluster1 As Cluster, ByVal cluster2 As Cluster) As Cluster
            Dim newItems As List(Of ISupportCoordLocation) = New List(Of ISupportCoordLocation)(cluster1.Items)
            newItems.AddRange(cluster2.Items)
            Return New Cluster(newItems)
        End Function

        Public Shared Function CalculateCenterPoint(ByVal items As List(Of ISupportCoordLocation)) As MapPoint
            Dim meanX As Double = 0
            Dim meanY As Double = 0
            For Each item As ISupportCoordLocation In items
                meanX += item.Location.GetX()
                meanY += item.Location.GetY()
            Next

            meanX /= items.Count
            meanY /= items.Count
            Return New MapPoint(meanX, meanY)
        End Function
    End Class
End Namespace
