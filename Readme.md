<!-- default file list -->
*Files to look at*:

* **[CureClusterer.cs](./CS/CustomClustererSample/CureClusterer.cs) (VB: [CureClusterer.vb](./VB/CustomClustererSample/CureClusterer.vb))**
* [MainWindow.xaml](./CS/CustomClustererSample/MainWindow.xaml) (VB: [MainWindow.xaml](./VB/CustomClustererSample/MainWindow.xaml))
<!-- default file list end -->
# How to implement a custom clusterer


This example demonstrates how to implement a custom clusterer.


<h3>Description</h3>

<p>To do this, inherit the&nbsp;<strong>MapClustererBase&nbsp;</strong>class and implement all abstract methods.&nbsp;<br />The&nbsp;<strong>Adapter.OnClustered</strong>&nbsp;method should be called to notify the Adapter that clustering is finished.<br />Note that to create a new collection of cluster representatives, the&nbsp;<strong>CreateItemColelction&nbsp;</strong>method should be used.</p>

<br/>


