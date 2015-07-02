using System;
using System.Collections.Generic;
using Verse;

namespace A2B
{
	// Return true to automatically deregister
	// DO NOT DEREGISTER FROM WITHIN A CALLBACK!
	// Invalidated lists are bad, mmm'kay?
	public delegate bool MonitorAction();

    public class A2BMonitor : MapComponent
    {
		private static Dictionary<string, MonitorAction> tickActions       = new Dictionary<string, MonitorAction>();
		private static Dictionary<string, MonitorAction> occasionalActions = new Dictionary<string, MonitorAction>();
		private static Dictionary<string, MonitorAction> updateActions     = new Dictionary<string, MonitorAction>();
		private static Dictionary<string, MonitorAction> guiActions        = new Dictionary<string, MonitorAction>();
		private static Dictionary<string, MonitorAction> exposeActions     = new Dictionary<string, MonitorAction>();

		private static List< string >                    removeKeys        = new List< string >();

		private void ProcessCallbacks( Dictionary<string, MonitorAction> d )
		{
			removeKeys.Clear();

			foreach (var p in d)
				if( p.Value() == true )
					removeKeys.Add( p.Key );

			foreach (var k in removeKeys)
				d.Remove( k );
		}

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

			ProcessCallbacks( updateActions );
        }

		public override void MapComponentTick()
		{
			base.MapComponentTick();

			ProcessCallbacks( tickActions );

			// Occasional only get fewer ticks so they don't bomb the system
			if( ( Find.TickManager.TicksGame + GetHashCode() ) % A2BData.OccasionalTicks == 0 )
				ProcessCallbacks( occasionalActions );
		}

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

			ProcessCallbacks( guiActions );
        }

        public override void ExposeData()
        {
            base.ExposeData();

			ProcessCallbacks( exposeActions );
        }

        #region Register/Deregister Actions

		public static void RegisterUpdateAction(string name, MonitorAction action)
        {
			if( !updateActions.ContainsKey( name ) )
            	updateActions.Add(name, action);
        }

        public static void DeregisterUpdateAction(string name)
        {
            updateActions.Remove(name);
        }

		public static void RegisterTickAction(string name, MonitorAction action)
		{
			if( !tickActions.ContainsKey( name ) )
				tickActions.Add(name, action);
		}

		public static void DeregisterTickAction(string name)
		{
			tickActions.Remove(name);
		}

		public static void RegisterOccasionalAction(string name, MonitorAction action)
		{
			if( !occasionalActions.ContainsKey( name ) )
				occasionalActions.Add(name, action);
		}

		public static void DeregisterOccasionalAction(string name)
		{
			occasionalActions.Remove(name);
		}

		public static void RegisterGUIAction(string name, MonitorAction action)
        {
			if( !guiActions.ContainsKey( name ) )
	            guiActions.Add(name, action);
        }

        public static void DeregisterGUIAction(string name)
        {
            guiActions.Remove(name);
        }

		public static void RegisterExposeDataAction(string name, MonitorAction action)
        {
			if( !exposeActions.ContainsKey( name ) )
	            exposeActions.Add(name, action);
        }

        public static void DeregisterExposeDataAction(string name)
        {
            exposeActions.Remove(name);
        }

        #endregion
    }
}
