namespace Mapbox.Unity.Utilities
{
	using System;

	public static class MapEvents
	{
		public static event Action<string> OnMapEvent = delegate(string eventName) { };

		public static void SendEvent(string eventName)
		{
			OnMapEvent(eventName);
		}
	}
}