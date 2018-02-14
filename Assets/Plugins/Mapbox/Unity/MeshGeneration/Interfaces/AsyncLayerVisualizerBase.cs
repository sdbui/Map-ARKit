namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using System.Collections;
	using Mapbox.VectorTile;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// Layer visualizers contains sytling logic and processes features
	/// </summary>
	public abstract class AsyncLayerVisualizerBase : ScriptableObject
	{
		public bool Active;
		public abstract string Key { get; set; }
		public abstract IEnumerator AsyncCreate(VectorTileLayer layer, UnityTile tile, Action cb);
	}
}
