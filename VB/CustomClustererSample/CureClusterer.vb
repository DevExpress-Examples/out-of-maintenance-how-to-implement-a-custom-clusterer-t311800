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
'INSTANT VB NOTE: The variable cluster was renamed since it may cause conflicts with calls to static members of the user-defined type with this name:
				Dim cluster_Conflict As Cluster = clusters(i)
				Dim representative As New MapDot() With {
					.Location = New GeoPoint(cluster_Conflict.CenterPoint.Y, cluster_Conflict.CenterPoint.X),
					.Size = 100
				}
				For j As Integer = 0 To cluster_Conflict.Items.Count - 1
					representative.ClusteredItems.Add(TryCast(cluster_Conflict.Items(j), MapItem))
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
			Dim i As Integer = 0
			Do While i < clusters.Count
'INSTANT VB NOTE: The variable cluster was renamed since it may cause conflicts with calls to static members of the user-defined type with this name:
				Dim cluster_Conflict As Cluster = clusters(i)
				AssignClosest(cluster_Conflict, clusters)
				' Inserts depending on distance to closest.
				Insert(arrangedClusters, cluster_Conflict)
				i += 1
			Loop
			Return arrangedClusters
		End Function

'INSTANT VB NOTE: The parameter cluster was renamed since it may cause conflicts with calls to static members of the user-defined type with this name:
		Private Shared Sub AssignClosest(ByVal cluster_Conflict As Cluster, ByVal clusters As List(Of Cluster))
			If clusters.Count < 2 Then
				Throw New ArgumentOutOfRangeException("Clusters count should be larger than 2.")
			End If
			Dim distancableCluster As Cluster = clusters(0)
			If distancableCluster Is cluster_Conflict Then
				distancableCluster = clusters(1)
			End If
			cluster_Conflict.ClosestCluster = distancableCluster

			For i As Integer = 0 To clusters.Count - 1
				distancableCluster = clusters(i)
				If distancableCluster Is cluster_Conflict Then
					Continue For
				End If
				Dim distance As Double = cluster_Conflict.DistanceTo(distancableCluster)
				If distance < cluster_Conflict.DistanceToClosest Then
					cluster_Conflict.ClosestCluster = distancableCluster
				End If
			Next i
		End Sub

'INSTANT VB NOTE: The parameter cluster was renamed since it may cause conflicts with calls to static members of the user-defined type with this name:
		Private Shared Sub Insert(ByVal clusters As List(Of Cluster), ByVal cluster_Conflict As Cluster)
			Dim i As Integer = 0
			Do While i < clusters.Count
				If clusters(i).DistanceToClosest > cluster_Conflict.DistanceToClosest Then
					clusters.Insert(i, cluster_Conflict)
					Return
				End If
				i += 1
			Loop
			clusters.Add(cluster_Conflict)
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
'INSTANT VB NOTE: The field centerPoint was renamed since Visual Basic does not allow fields to have the same name as other class members:
		Private centerPoint_Conflict As MapPoint
'INSTANT VB NOTE: The field items was renamed since Visual Basic does not allow fields to have the same name as other class members:
		Private items_Conflict As List(Of ISupportCoordLocation)
'INSTANT VB NOTE: The field closestCluster was renamed since Visual Basic does not allow fields to have the same name as other class members:
		Private closestCluster_Conflict As Cluster
'INSTANT VB NOTE: The field distanceToClosest was renamed since Visual Basic does not allow fields to have the same name as other class members:
		Private distanceToClosest_Conflict As Double

		Public Sub New(ByVal items As List(Of ISupportCoordLocation))
			Me.items_Conflict = items
			centerPoint_Conflict = CalculateCenterPoint(items)
		End Sub


		Public Shared Function Initialize(ByVal item As ISupportCoordLocation) As Cluster
'INSTANT VB NOTE: The variable items was renamed since Visual Basic does not handle local variables named the same as class members well:
			Dim items_Conflict As New List(Of ISupportCoordLocation)()
			items_Conflict.Add(item)
			Return New Cluster(items_Conflict)
		End Function

		Public ReadOnly Property CenterPoint() As MapPoint
			Get
				Return Me.centerPoint_Conflict
			End Get
		End Property
		Public ReadOnly Property Items() As List(Of ISupportCoordLocation)
			Get
				Return Me.items_Conflict
			End Get
		End Property

		Public Property ClosestCluster() As Cluster
			Get
				Return Me.closestCluster_Conflict
			End Get
			Set(ByVal value As Cluster)
				Me.closestCluster_Conflict = value
				distanceToClosest_Conflict = DistanceTo(closestCluster_Conflict)
			End Set
		End Property

		Public ReadOnly Property DistanceToClosest() As Double
			Get
				Return distanceToClosest_Conflict
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
