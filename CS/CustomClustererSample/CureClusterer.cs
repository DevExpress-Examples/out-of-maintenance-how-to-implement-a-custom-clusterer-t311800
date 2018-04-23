using DevExpress.Map;
using DevExpress.Xpf.Map;
using System;
using System.Collections.Generic;

namespace CustomClustererSample {
    class CureClusterer : MapClustererBase {
        MapVectorItemCollection currentItems;

        public override MapVectorItemCollection Items { get { return currentItems; } }

        public int ClusterCount { get; set; }

        public override void Clusterize(MapVectorItemCollection sourceItems, MapViewport viewport, bool sourceChanged) {
            if (sourceChanged) {
                currentItems = ClusterizeImpl(sourceItems);
                Adapter.OnClustered();
            }
        }

        protected override MapDependencyObject CreateObject() {
            return new CureClusterer();
        }

        MapVectorItemCollection ClusterizeImpl(MapVectorItemCollection sourceItems) {
            // Separate localizable and non localizable items.
            List<MapItem> nonLocalizableItems = new List<MapItem>();
            List<Cluster> clusters = new List<Cluster>();
            foreach (MapItem item in sourceItems) {
                ISupportCoordLocation localizableItem = item as ISupportCoordLocation;
                if (localizableItem != null)
                    clusters.Add(Cluster.Initialize(localizableItem));
                else
                    nonLocalizableItems.Add(item);
            }

            // Arrange initial clusters in increasing order of distance to a closest cluster.
            clusters = Arrange(clusters);

            // Aggregate localizable items.
            while (clusters.Count > ClusterCount) {
                MergeCloserstClusters(ref clusters);
            }

            // Convert internal cluster helpers to Map items.
            MapVectorItemCollection clusterRepresentatives = CreateItemsCollection();
            for (int i = 0; i < clusters.Count; ++i) {
                Cluster cluster = clusters[i];
                MapDot representative = new MapDot() { Location = new GeoPoint(cluster.CenterPoint.Y, cluster.CenterPoint.X), Size = 100 };
                for (int j = 0; j < cluster.Items.Count; ++j)
                    representative.ClusteredItems.Add(cluster.Items[j] as MapItem);
                clusterRepresentatives.Add(representative);
            }
            for (int i = 0; i < nonLocalizableItems.Count; ++i)
                clusterRepresentatives.Add(nonLocalizableItems[i]);
            return clusterRepresentatives;
        }

        static List<Cluster> Arrange(List<Cluster> clusters) {
            List<Cluster> arrangedClusters = new List<Cluster>();
            for (int i = 0; i < clusters.Count; ++i) {
                Cluster cluster = clusters[i];
                AssignClosest(cluster, clusters);
                // Inserts depending on distance to closest.
                Insert(arrangedClusters, cluster);
            }
            return arrangedClusters;
        }

        static void AssignClosest(Cluster cluster, List<Cluster> clusters) {
            if (clusters.Count < 2) throw new ArgumentOutOfRangeException("Clusters count should be larger than 2.");
            Cluster distancableCluster = clusters[0];
            if (distancableCluster == cluster)
                distancableCluster = clusters[1];
            cluster.ClosestCluster = distancableCluster;

            for (int i = 0; i < clusters.Count; ++i) {
                distancableCluster = clusters[i];
                if (distancableCluster == cluster) continue;
                double distance = cluster.DistanceTo(distancableCluster);
                if (distance < cluster.DistanceToClosest)
                    cluster.ClosestCluster = distancableCluster;
            }
        }

        static void Insert(List<Cluster> clusters, Cluster cluster) {
            for (int i = 0; i < clusters.Count; ++i) {
                if (clusters[i].DistanceToClosest > cluster.DistanceToClosest) {
                    clusters.Insert(i, cluster);
                    return;
                }
            }
            clusters.Add(cluster);
        }

        static void MergeCloserstClusters(ref List<Cluster> clusters) {
            if (clusters.Count < 2) throw new ArgumentOutOfRangeException("Clusters count should be larger than 2.");
            Cluster cluster1 = clusters[0];
            Cluster cluster2 = cluster1.ClosestCluster;
            clusters.RemoveAt(0);
            clusters.Remove(cluster2);
            Cluster newCluster = Cluster.Merge(cluster1, cluster2);
            clusters.Add(newCluster);
            clusters = Arrange(clusters);
        }
    }

    struct MapPoint {
        public MapPoint(double x, double y) {
            X = x;
            Y = y;
        }
        public double X { get; set; }
        public double Y { get; set; }
    }

    class Cluster {
        MapPoint centerPoint;
        List<ISupportCoordLocation> items;
        Cluster closestCluster;
        double distanceToClosest;

        public Cluster(List<ISupportCoordLocation> items) {
            this.items = items;
            centerPoint = CalculateCenterPoint(items);
        }


        public static Cluster Initialize(ISupportCoordLocation item) {
            List<ISupportCoordLocation> items = new List<ISupportCoordLocation>();
            items.Add(item);
            return new Cluster(items);
        }

        public MapPoint CenterPoint { get { return this.centerPoint; } }
        public List<ISupportCoordLocation> Items { get { return this.items; } }

        public Cluster ClosestCluster {
            get { return this.closestCluster; }
            set {
                this.closestCluster = value;
                distanceToClosest = DistanceTo(closestCluster);
            }
        }

        public double DistanceToClosest { get { return distanceToClosest; } }

        public double DistanceTo(Cluster h) {
            return Math.Sqrt((h.CenterPoint.X - CenterPoint.X) * (h.CenterPoint.X - CenterPoint.X) +
                             (h.CenterPoint.Y - CenterPoint.Y) * (h.CenterPoint.Y - CenterPoint.Y));
        }

        public static Cluster Merge(Cluster cluster1, Cluster cluster2) {
            List<ISupportCoordLocation> newItems = new List<ISupportCoordLocation>(cluster1.Items);
            newItems.AddRange(cluster2.Items);

            return new Cluster(newItems);
        }

        public static MapPoint CalculateCenterPoint(List<ISupportCoordLocation> items) {
            double meanX = 0;
            double meanY = 0;
            foreach (ISupportCoordLocation item in items) {
                meanX += item.Location.GetX();
                meanY += item.Location.GetY();
            }
            meanX /= items.Count;
            meanY /= items.Count;
            return new MapPoint(meanX, meanY);
        }
    }
}
