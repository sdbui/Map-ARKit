using System;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Mapbox.Unity.Map;

public class WrappingTileProvider : AbstractTileProvider
{
	[SerializeField]
	Transform _mapPanTransform;

	Vector4 _range = new Vector4(1, 1, 1, 1);
	UnwrappedTileId _centerTile;

	Vector2d? _initialTileSizeMeters;
	public Vector3 MapCenterUnityPos
	{
		get
		{
			var centerBounds = Conversions.TileBounds(_centerTile);
			Vector2d centerMeters = centerBounds.Center;
			Vector2d centerOffsetMeters = new Vector2d(
				centerMeters.x - _map.CenterMercator.x,
				centerMeters.y - _map.CenterMercator.y
			);

			if (_initialTileSizeMeters == null)
				_initialTileSizeMeters = centerBounds.Size;

			Vector2d mapCenterUnityPos = new Vector2d(
				(centerOffsetMeters.x / _initialTileSizeMeters.Value.x) * _map.UnityTileSize,
				(centerOffsetMeters.y / _initialTileSizeMeters.Value.y) * _map.UnityTileSize
             );
			return mapCenterUnityPos.ToVector3xz();
		}
	}

	/* * * * * *
     * 
     * Initialization
     * 
     * * * * * */

	protected override void OnInitialized()
	{
		ResetTiles(_map.CenterLatitudeLongitude, _map.Zoom);
	}

	/* * * * * *
     * 
     * Public Methods
     * 
     * * * * * */

	[ContextMenu("Wrap Up")]
	public void WrapMapUp()
	{
		RunActionOnRow((int)_range.w, RemoveTile);
		RunActionOnRow((int)-_range.y - 1, AddTile);
		_centerTile = new UnwrappedTileId(_map.Zoom, _centerTile.X, _centerTile.Y - 1);
	}

	[ContextMenu("Wrap Down")]
	public void WrapMapDown()
	{
		RunActionOnRow((int)-_range.y, RemoveTile);
		RunActionOnRow((int)_range.w + 1, AddTile);
		_centerTile = new UnwrappedTileId(_map.Zoom, _centerTile.X, _centerTile.Y + 1);
	}

	[ContextMenu("Wrap Left")]
	public void WrapMapLeft()
	{
		RunActionOnColumn((int)_range.z, RemoveTile);
		RunActionOnColumn((int)-_range.x - 1, AddTile);
		_centerTile = new UnwrappedTileId(_map.Zoom, _centerTile.X - 1, _centerTile.Y);
	}

	[ContextMenu("Wrap Right")]
	public void WrapMapRight()
	{
		RunActionOnColumn((int)-_range.x, RemoveTile);
		RunActionOnColumn((int)_range.z + 1, AddTile);
		_centerTile = new UnwrappedTileId(_map.Zoom, _centerTile.X + 1, _centerTile.Y);
	}

	[ContextMenu("Zoom In")]
	public void ZoomIn()
	{		
		if (_map.Zoom >= 17)
		{
			return;
		}

		ResetTiles(GetCurrentLatLon(), _map.Zoom + 1);
	}

	[ContextMenu("Zoom Out")]
	public void ZoomOut()
	{
		if (_map.Zoom <= 11)
		{
			return;
		}

		ResetTiles(GetCurrentLatLon(), _map.Zoom - 1);
	}

	/* * * * * *
     * 
     * Private Methods
     * 
     * * * * * */

	private Vector2d GetCurrentLatLon()
	{
		Vector2d centerMeters = Conversions.TileBounds(_centerTile).Center;
		Vector2d centerOffsetMeters = new Vector2d(
			centerMeters.x - _map.CenterMercator.x,
			centerMeters.y - _map.CenterMercator.y
		);
		Vector2d panOffsetMeters = new Vector2d(
			_mapPanTransform.localPosition.x / _map.WorldRelativeScale,
			_mapPanTransform.localPosition.z / _map.WorldRelativeScale
		);

		Vector2d latlon = Conversions.MetersToLatLon(centerMeters - centerOffsetMeters - panOffsetMeters);
		return latlon;
	}

	private void ResetTiles(Vector2d latlon, int zoom)
	{
		// set the new zoom for the map
		_map.Zoom = zoom;

		// remove all tiles from the map
		while (_activeTiles.Count > 0)
		{
			RemoveTile(_activeTiles[0]);
		}

		// create one center tile and add surrounding tiles defined by "_range"
		_centerTile = TileCover.CoordinateToTileId(latlon, _map.Zoom);

		for (int x = (int)(_centerTile.X- _range.x); x <= (_centerTile.X + _range.z); x++)
		{
			for (int y = (int)(_centerTile.Y - _range.y); y <= (_centerTile.Y + _range.w); y++)
			{
				AddTile(new UnwrappedTileId(_map.Zoom, x, y));
			}
		}
	}

	private void RunActionOnColumn(int x, Action<UnwrappedTileId> fn)
	{
		// convert relative x to absolute x
		x = _centerTile.X + x;

		int yMin = (int)(_centerTile.Y - _range.y);
		int yMax = (int)(_centerTile.Y + _range.w);

		for (int y = yMin; y <= yMax; y++)
		{
			fn(new UnwrappedTileId(_map.Zoom, x, y));
		}
	}

	private void RunActionOnRow(int y, Action<UnwrappedTileId> fn)
	{
		// convert relative y to absolute y
		y = _centerTile.Y + y;

		int xMin = (int)(_centerTile.X - _range.x);
		int xMax = (int)(_centerTile.X + _range.z);

		for (int x = xMin; x <= xMax; x++)
		{
			fn(new UnwrappedTileId(_map.Zoom, x, y));
		}
	}
}
