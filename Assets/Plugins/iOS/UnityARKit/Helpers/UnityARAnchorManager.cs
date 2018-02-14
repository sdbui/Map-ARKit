using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.XR.iOS
{
	public class UnityARAnchorManager 
	{


		private Dictionary<string, ARPlaneAnchorGameObject> planeAnchorMap;
		private bool mapBorderVisible = true;


        public UnityARAnchorManager ()
		{
			planeAnchorMap = new Dictionary<string,ARPlaneAnchorGameObject> ();
			UnityARSessionNativeInterface.ARAnchorAddedEvent += AddAnchor;
			UnityARSessionNativeInterface.ARAnchorUpdatedEvent += UpdateAnchor;
			UnityARSessionNativeInterface.ARAnchorRemovedEvent += RemoveAnchor;

		}


		public void AddAnchor(ARPlaneAnchor arPlaneAnchor)
		{
			GameObject go = UnityARUtility.CreatePlaneInScene (arPlaneAnchor);
			go.AddComponent<DontDestroyOnLoad> ();  //this is so these GOs persist across scene loads
			ARPlaneAnchorGameObject arpag = new ARPlaneAnchorGameObject ();
			arpag.planeAnchor = arPlaneAnchor;
			arpag.gameObject = go;
			planeAnchorMap.Add (arPlaneAnchor.identifier, arpag);

			// hide or show the newly created plane
			ToggleVisibility(mapBorderVisible);
		}

		public void RemoveAnchor(ARPlaneAnchor arPlaneAnchor)
		{
			if (planeAnchorMap.ContainsKey (arPlaneAnchor.identifier)) {
				ARPlaneAnchorGameObject arpag = planeAnchorMap [arPlaneAnchor.identifier];
				GameObject.Destroy (arpag.gameObject);
				planeAnchorMap.Remove (arPlaneAnchor.identifier);
			}
		}

		public void UpdateAnchor(ARPlaneAnchor arPlaneAnchor)
		{
			if (planeAnchorMap.ContainsKey (arPlaneAnchor.identifier)) {
				ARPlaneAnchorGameObject arpag = planeAnchorMap [arPlaneAnchor.identifier];
				UnityARUtility.UpdatePlaneWithAnchorTransform (arpag.gameObject, arPlaneAnchor);
				arpag.planeAnchor = arPlaneAnchor;
				planeAnchorMap [arPlaneAnchor.identifier] = arpag;
			}
		}

        public void Destroy()
        {
            foreach (ARPlaneAnchorGameObject arpag in GetCurrentPlaneAnchors()) {
                GameObject.Destroy (arpag.gameObject);
            }

            planeAnchorMap.Clear ();
        }

		public void ToggleVisibility()
		{
			mapBorderVisible = !mapBorderVisible;
			ToggleVisibility(mapBorderVisible);
		}

		private void ToggleVisibility(bool visible)
		{
			foreach (ARPlaneAnchorGameObject arpag in GetCurrentPlaneAnchors())
			{
				arpag.gameObject.GetComponentInChildren<MeshRenderer>().enabled = visible;
			}
		}

		public List<ARPlaneAnchorGameObject> GetCurrentPlaneAnchors()
		{
			return planeAnchorMap.Values.ToList ();
		}
	}
}

