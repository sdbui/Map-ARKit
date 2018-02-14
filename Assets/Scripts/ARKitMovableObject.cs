using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using SocketIO;

namespace UnityEngine.XR.iOS
{
	[RequireComponent(typeof(AbstractMap))]
	[RequireComponent(typeof(WrappingTileProvider))]
	public class ARKitMovableObject : MonoBehaviour
	{
		public Transform mapZoomTransform;
		public Transform mapPanTransform;
		public Transform parentTransform;
		public Transform m_HitTransform;
		public float scaleDuration = 1.0f; // how long does it take for the mesh to shrink/grow

		private AbstractMap abstractMap;
		private WrappingTileProvider wrappingTileProvider;
		private Material[] mapMaterials;
		private List<Material> mapTileMaterials = new List<Material>();

		private Vector3 startScale;
		private IEnumerator scalingCoroutine;

		private float xAnchor;
		private float yAnchor;
		private float initialTileSize, tileSize;
		private bool isPlacing = true;

		// map panning variables
		private Plane mapPlane;
		private Vector3? prevMapPlanePos;

		// web socket variables
		private GameObject socketObject;
		private SocketIOComponent socket;
		private float socketTimeout = 1.0f;

		// map zooming variables
		private Vector3 prevZoomScale;
		private float? prevPinchMagnitude;

		/* * * * * *
	     * 
	     * Unity Lifecycle Methods
	     * 
	     * * * * * */

		void Awake()
		{
			abstractMap = GetComponent<AbstractMap>();
			wrappingTileProvider = GetComponent<WrappingTileProvider>();

			// save the initial scale, then set it to zero
			startScale = parentTransform.localScale;
#if !UNITY_EDITOR
			parentTransform.localScale = Vector3.zero;
#endif

			// register for the "Initialized" event to get the tile size
			abstractMap.OnInitialized += AbstractMap_OnInitialized;
			abstractMap.OnTileLoaded += AbstractMap_OnTileLoaded;

			mapPlane = new Plane(m_HitTransform.up, m_HitTransform.position);

			// get references to all the map materials
			Object[] uncastMapMaterials = Resources.LoadAll("Materials", typeof(Material));

			mapMaterials = new Material[uncastMapMaterials.Length];
			for (int i = 0; i < uncastMapMaterials.Length; i++)
			{
				mapMaterials[i] = (Material)uncastMapMaterials[i];
			}
		}

		void Start()
		{
#if UNITY_EDITOR
			//if running in unity, force to re input url every time
			PlayerPrefs.DeleteKey("socket_url");
#endif
			var webSocketPanel = GameObject.Find("SocketPanel");
			var canvasGroup = webSocketPanel.GetComponent<CanvasGroup> ();
			if (string.IsNullOrEmpty(PlayerPrefs.GetString("socket_url"))) 
			{
				canvasGroup.alpha = 1;
			}
			StartCoroutine(SocketIORoutine());
		}

		// Update is called once per frame
		void Update() 
		{
#if UNITY_EDITOR
			GetMapMaterialReferences();
			SetMapMaterialClipping();
#endif

			// prevent UI presses from propagating to this script
			// TODO - find a better way to do this
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			else if (Input.touchCount > 0)
			{
				if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
				{
					return;
				}
			}

			if (!isPlacing)
			{
#if UNITY_EDITOR
				if (Input.GetMouseButtonDown (0))
#else
				if (Input.GetMouseButtonDown (0) && Input.touchCount == 1)
#endif
				{
					prevMapPlanePos = GetMapPlaneRaycastPos(Input.mousePosition);
				}
#if UNITY_EDITOR
				if (Input.GetMouseButton (0))
#else
				if (Input.GetMouseButton (0) && Input.touchCount == 1)
#endif
				{
					// do nothing if either the current or previous plane positions are null
					Vector3? currentMapPlanePos = GetMapPlaneRaycastPos(Input.mousePosition);
					if (!prevMapPlanePos.HasValue || !currentMapPlanePos.HasValue)
					{
						return;
					}

					// move the map by the difference between current and previous plane positions
					Vector3 deltaMapPlanePos = currentMapPlanePos.Value - prevMapPlanePos.Value;
					deltaMapPlanePos.y = 0;
					mapPanTransform.position += deltaMapPlanePos;


					if (socket != null) 
					{
						// todo: revisit trying to use m_HitTransform.localPosition instead of
						// subtracting the parentTransform's geoPosition. Was getting undesireable results so this will do for now.
						var latLng = m_HitTransform.position.GetGeoPosition(new Vector2d(0,0), .0008f);
						var parentLatLng = parentTransform.position.GetGeoPosition (new Vector2d(0,0), .0008f);

						// x and y from the return value of GetGeoPosition contain longitude and latitude respectively
						Dictionary<string, string> data = new Dictionary<string, string>() { { "x", (latLng.y - parentLatLng.y).ToString() }, { "y", (latLng.x - parentLatLng.x).ToString() } };
						socket.Emit("webMousemove", new JSONObject(data));	
					}

					// save the current plane position for next frame
					prevMapPlanePos = currentMapPlanePos;

					if (mapPanTransform.localPosition.x < xAnchor - tileSize)
					{
						wrappingTileProvider.WrapMapRight();
						xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
					}
					if (mapPanTransform.localPosition.x > xAnchor + tileSize)
					{
						wrappingTileProvider.WrapMapLeft();
						xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
					}

					if (mapPanTransform.localPosition.z < yAnchor - tileSize)
					{
						wrappingTileProvider.WrapMapUp();
						yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
					}
					if (mapPanTransform.localPosition.z > yAnchor + tileSize)
					{
						wrappingTileProvider.WrapMapDown();
						yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
					}
				}
				if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Minus))
				{
					if (Input.GetKey(KeyCode.Equals))
						mapZoomTransform.localScale += mapZoomTransform.localScale * 0.1f;
					if (Input.GetKey(KeyCode.Minus))
						mapZoomTransform.localScale -= mapZoomTransform.localScale * 0.1f;

					if (mapZoomTransform.localScale.x < prevZoomScale.x / 2f)
					{
						wrappingTileProvider.ZoomOut();
						prevZoomScale /= 2f;
						xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
						yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
						tileSize = GameObject.FindGameObjectWithTag("MapTile").GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * m_HitTransform.localScale.x;

					}
					if (mapZoomTransform.localScale.x > prevZoomScale.x * 2f)
					{
						wrappingTileProvider.ZoomIn();
						prevZoomScale *= 2f;
						xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
						yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
						tileSize = GameObject.FindGameObjectWithTag("MapTile").GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * m_HitTransform.localScale.x;

					}
				}
				if (Input.touchCount == 2)
				{
					if (prevPinchMagnitude == null)
					{
						var touches = new Vector2[] 
						{
							Camera.main.ScreenToViewportPoint(Input.touches[0].position),
							Camera.main.ScreenToViewportPoint(Input.touches[1].position),
						};

						prevPinchMagnitude = (touches[1] - touches[0]).magnitude;
					}
					else
					{
						var touches = new Vector2[] 
						{
							Camera.main.ScreenToViewportPoint(Input.touches[0].position),
							Camera.main.ScreenToViewportPoint(Input.touches[1].position),
						};

						float pinchDelta = (touches[1] - touches[0]).magnitude - prevPinchMagnitude.Value;
						mapZoomTransform.localScale += mapZoomTransform.localScale * pinchDelta * 2f;
						if (mapZoomTransform.localScale.x < 0.04195315f)
						{
							mapZoomTransform.localScale = Vector3.one * 0.04195315f;
						}
						if (mapZoomTransform.localScale.x > 2.0f)
						{
							mapZoomTransform.localScale = Vector3.one * 2.0f;
						}

						prevPinchMagnitude = (touches[1] - touches[0]).magnitude;

						if (mapZoomTransform.localScale.x < prevZoomScale.x / 2f && abstractMap.Zoom > 11)
						{
							wrappingTileProvider.ZoomOut();
							prevZoomScale /= 2f;
							xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
							yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
							tileSize = GameObject.FindGameObjectWithTag("MapTile").GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * m_HitTransform.localScale.x;

						}
						if (mapZoomTransform.localScale.x > prevZoomScale.x * 2f && abstractMap.Zoom < 17)
						{
							wrappingTileProvider.ZoomIn();
							prevZoomScale *= 2f;
							xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
							yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
							tileSize = GameObject.FindGameObjectWithTag("MapTile").GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * m_HitTransform.localScale.x;

						}
					}
				}
				if (Input.touchCount != 2)
				{
					prevPinchMagnitude = null;
				}
			}
			else
			{
				if (Input.touchCount > 0 && m_HitTransform != null)
				{
					var touch = Input.GetTouch(0);
					if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
					{
						var screenPosition = Camera.main.ScreenToViewportPoint(touch.position);
						ARPoint point = new ARPoint {
							x = screenPosition.x,
							y = screenPosition.y
						};

						// prioritize reults types
						ARHitTestResultType[] resultTypes = {
							ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, 
							// if you want to use infinite planes use this:
							//ARHitTestResultType.ARHitTestResultTypeExistingPlane,
							ARHitTestResultType.ARHitTestResultTypeHorizontalPlane, 
							// ARHitTestResultType.ARHitTestResultTypeFeaturePoint
						}; 

						foreach (ARHitTestResultType resultType in resultTypes)
						{
							if (HitTestWithResultType (point, resultType))
							{
								// plane hit, show the map
								ShowMesh();
								return;
							}
						}

						// no plane hits, hide the map
						HideMesh();
					}
				}
			}
		}

		/* * * * * *
	     * 
	     * Public Methods
	     * 
	     * * * * * */

		public void ToggleInputMode()
		{
			isPlacing = !isPlacing;
		}

		public void RefreshMaterialReferences()
		{
			GameObject[] mapTiles = GameObject.FindGameObjectsWithTag("MapTile");
			foreach (var mapTile in mapTiles)
			{
				var renderer = mapTile.GetComponent<MeshRenderer>();
				mapTileMaterials.AddRange(renderer.materials);
			}
		}

		public void SaveSocketUrl()
		{
			GameObject socketField = GameObject.Find("SocketUrlInput");
			var inputField = socketField.GetComponent<InputField>();
			PlayerPrefs.SetString("socket_url", inputField.text);
		}

		public void SkipSocketUrl()
		{
			var panel = GameObject.Find("SocketPanel").GetComponent<CanvasGroup>(); 
			panel.alpha = 0;
			panel.interactable = false;
			panel.blocksRaycasts = false;
		}

		/* * * * * *
	     * 
	     * Private Methods
	     * 
	     * * * * * */

		bool HitTestWithResultType(ARPoint point, ARHitTestResultType resultTypes)
		{
			List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, resultTypes);
			if (hitResults.Count > 0) 
			{
				foreach (var hitResult in hitResults) 
				{
					parentTransform.position = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
					parentTransform.position += new Vector3(0, 0.1f, 0);
					parentTransform.rotation = UnityARMatrixOps.GetRotation(hitResult.worldTransform);
					GetMapMaterialReferences();
					SetMapMaterialClipping();

					mapPlane = new Plane(m_HitTransform.up, m_HitTransform.position);
					return true;
				}
			}
			return false;
		}

		void AbstractMap_OnInitialized()
		{
			var go = GameObject.FindGameObjectWithTag("MapTile");
			tileSize = initialTileSize = go.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * m_HitTransform.localScale.x;
			xAnchor = -wrappingTileProvider.MapCenterUnityPos.x;
			yAnchor = -wrappingTileProvider.MapCenterUnityPos.z;
			prevZoomScale = mapZoomTransform.localScale;

			GameObject[] mapTiles = GameObject.FindGameObjectsWithTag("MapTile");
			foreach (var mapTile in mapTiles)
			{
				var renderer = mapTile.GetComponent<MeshRenderer>();
				mapTileMaterials.AddRange(renderer.materials);
			}
		}

		// TODO Get rid of this
//		bool called = false;
		void AbstractMap_OnTileLoaded()
		{
			RefreshMaterialReferences();
		}

		/// <summary>
		/// Raycast from the main camera to the map plane, using the current "Input.mousePosition".
		/// If the raycast hits, return the position of the hit
		/// </summary>
		/// <returns>The world space position of the raycast hit (if hit)</returns>
		Vector3? GetMapPlaneRaycastPos(Vector3 pointerPosition)
		{
			Vector3? hitPos = null;
			float rayDistance;

			Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
			if (mapPlane.Raycast(ray, out rayDistance))
			{
				hitPos = ray.GetPoint(rayDistance);
			}

			return hitPos;
		}

		void GetMapMaterialReferences()
		{
//			mapMaterials.Clear();
//
//			GameObject[] mapTiles = GameObject.FindGameObjectsWithTag("MapTile");
//			foreach (var mapTile in mapTiles)
//			{
//				var renderer = mapTile.GetComponent<MeshRenderer>();
//				mapMaterials.AddRange(renderer.sharedMaterials);
//			}
//
//			GameObject[] mapMeshes = GameObject.FindGameObjectsWithTag("MapMesh");
//			foreach (var mapMesh in mapMeshes)
//			{
//				var renderer = mapMesh.GetComponent<MeshRenderer>();
//				mapMaterials.AddRange(renderer.sharedMaterials);
//			}
		}

		void SetMapMaterialClipping()
		{
			foreach (var material in mapMaterials.Concat(mapTileMaterials))
			{
				material.SetFloat("_xClipMin", parentTransform.position.x - initialTileSize);
				material.SetFloat("_xClipMax", parentTransform.position.x + initialTileSize);
				material.SetFloat("_zClipMin", parentTransform.position.z - initialTileSize);
				material.SetFloat("_zClipMax", parentTransform.position.z + initialTileSize);
				material.SetFloat("_xzRotation", (float)(-m_HitTransform.eulerAngles.y / 180.0f * Math.PI));
			}
		}

		/// <summary>
		//  Scale the mesh up if it's being animated to hidden or is already hidden
		/// </summary>
		[ContextMenu("ShowMesh")]
		void ShowMesh()
		{
			if (parentTransform.localScale != startScale)
			{
				if (scalingCoroutine != null)
				{
					StopCoroutine(scalingCoroutine);
				}

				scalingCoroutine = Scale(parentTransform.localScale, startScale);
				StartCoroutine(scalingCoroutine);
			}
		}

		/// <summary>
		/// Scale the mesh down if it's being animated to shown or is already shown
		/// </summary>
		[ContextMenu("HideMesh")]
		void HideMesh()
		{
			if (parentTransform.localScale != Vector3.zero)
			{
				if (scalingCoroutine != null)
				{
					StopCoroutine(scalingCoroutine);
				}

				scalingCoroutine = Scale(parentTransform.localScale, Vector3.zero);
				StartCoroutine(scalingCoroutine);
			}
		}

		/// <summary>
		/// Animate the scale of this transform from "startScale" to "endScale" over
		/// the course of "scaleDuration" seconds
		/// </summary>
		/// <param name="startScale">Start scale.</param>
		/// <param name="endScale">End scale.</param>
		IEnumerator Scale(Vector3 startScale, Vector3 endScale)
		{
			parentTransform.localScale = startScale;

			float t = Time.time;
			while (Time.time - t < scaleDuration)
			{
				float p = Mathf.SmoothStep(0, 1, (Time.time - t) / scaleDuration);
				parentTransform.localScale = Vector3.Lerp(startScale, endScale, p);
				yield return null;
			}

			parentTransform.localScale = endScale;
			scalingCoroutine = null;
		}
			
		/// <summary>
		/// Initializes the socketIo game object and component
		/// </summary>
		IEnumerator SocketIORoutine()
		{
			while (string.IsNullOrEmpty(PlayerPrefs.GetString("socket_url")))
				yield return null;
			GameObject.Find("SocketErrorText").GetComponent<Text>().enabled = false;
			socketObject = new GameObject("SocketIO");
			socketObject.AddComponent<SocketIOComponent>();
			socket = socketObject.GetComponent<SocketIOComponent>();
			socket.Url = "ws://" + PlayerPrefs.GetString ("socket_url") + "/socket.io/?EIO=4&transport=websocket";
			socket.On("connect", SocketConnectCb);
			socket.Connect();

			// if no connection within appropriate time, destroy and retry
			yield return new WaitForSeconds(socketTimeout);
			if (!socket.socket.IsConnected) 
			{
				PlayerPrefs.SetString("socket_url", "");
				GameObject.Destroy(socketObject);
				GameObject.Find("SocketUrlInput").GetComponent<InputField>().text = "";
				GameObject.Find("SocketPanel").GetComponent<CanvasGroup>().alpha = 1;
				GameObject.Find("SocketErrorText").GetComponent<Text>().enabled = true;
				StartCoroutine(SocketIORoutine());
			}
		}

		void SocketConnectCb(SocketIOEvent e)
		{
			var group = GameObject.Find("SocketPanel").GetComponent<CanvasGroup>();
			group.alpha = 0;
			group.blocksRaycasts = false;
			Dictionary<string, string> data = new Dictionary<string, string>();
			data["message"] = "Hello from unity!";
			socket.Emit("test", new JSONObject(data));
		}
	}
}

