namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;

	/// <summary>
	/// Texture Modifier is a basic modifier which simply adds a TextureSelector script to the features.
	/// Logic is all pushed into this TextureSelector mono behaviour to make it's easier to change it in runtime.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Crime Rate Texture Modifier")]
	public class CrimeRateTextureModifier : GameObjectModifier
	{
		[SerializeField]
		private List<MaterialRange> _materialRanges;

		public override void Run(FeatureBehaviour fb)
		{
			var ts = fb.gameObject.AddComponent<TextureSelector>();

			// find the "height" property for this feature
			int height = 0;

			if (fb.Data.Properties.ContainsKey("crimeIndex"))
			{
				if (int.TryParse(fb.Data.Properties["crimeIndex"].ToString(), out height))
				{
				}
			}

			// find the top and side materials for this height; start with arbitrary defaults
			Material topMaterial = _materialRanges[0].topMaterial;
			Material sideMaterial = _materialRanges[0].sideMaterial;

			foreach (var materialRange in _materialRanges)
			{
				if (materialRange.min <= height && materialRange.max >= height)
				{
					topMaterial = materialRange.topMaterial;
					sideMaterial = materialRange.sideMaterial;
					break;
				}
			}

			Material[] topMaterials = new Material[] { topMaterial };
			Material[] sideMaterials = new Material[] { sideMaterial };
			ts.Initialize(fb, true, false, topMaterials, true, sideMaterials);
		}

		[Serializable]
		private struct MaterialRange
		{
			public float min;
			public float max;
			public Material topMaterial;
			public Material sideMaterial;
		}
	}
}
