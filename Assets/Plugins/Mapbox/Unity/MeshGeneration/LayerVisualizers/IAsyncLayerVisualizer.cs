namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using Mapbox.VectorTile;
	using Mapbox.Unity.MeshGeneration.Data;

	public interface IAsyncLayerVisualizer 
	{
		void AsyncCreate(VectorTileLayer layer, UnityTile tile, Action cb);
	}
}
