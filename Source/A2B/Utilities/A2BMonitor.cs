using System;
using System.Collections.Generic;
using Verse;

namespace A2B
{
	// Return true to automatically deregister
	// DO NOT DEREGISTER FROM WITHIN A CALLBACK!
	// Invalidated lists are bad, mmm'kay?
	public delegate bool MonitorAction( object target );

    public class A2BMonitor : MapComponent
    {
        private static Dictionary<string, Pair<MonitorAction,object>> tickActions       = new Dictionary<string, Pair<MonitorAction,object>>();
        private static Dictionary<string, Pair<MonitorAction,object>> occasionalActions = new Dictionary<string, Pair<MonitorAction,object>>();
        private static Dictionary<string, Pair<MonitorAction,object>> updateActions     = new Dictionary<string, Pair<MonitorAction,object>>();
        private static Dictionary<string, Pair<MonitorAction,object>> guiActions        = new Dictionary<string, Pair<MonitorAction,object>>();
        private static Dictionary<string, Pair<MonitorAction,object>> exposeActions     = new Dictionary<string, Pair<MonitorAction,object>>();

		private static List< string >                    removeKeys        = new List< string >();

        private bool                                     firstTick         = true;

		private void ProcessCallbacks( Dictionary<string, Pair<MonitorAction,object>> d )
		{
			removeKeys.Clear();

			foreach( var p in d )
                if( p.Value.First( p.Value.Second ) == true )
					removeKeys.Add( p.Key );

			foreach( var k in removeKeys )
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

			// Occasional get fewer ticks so they don't bomb the system
            if( ( firstTick == true )||
                ( ( Find.TickManager.TicksGame + GetHashCode() ) % A2BData.OccasionalTicks == 0 ) )
				ProcessCallbacks( occasionalActions );

            firstTick = false;
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

		public static void RegisterUpdateAction( string name, MonitorAction action, object target = null )
        {
			if( !updateActions.ContainsKey( name ) )
                updateActions.Add( name, new Pair<MonitorAction, object>( action, target ) );
        }

        public static void DeregisterUpdateAction( string name )
        {
            updateActions.Remove( name );
        }

        public static void RegisterTickAction( string name, MonitorAction action, object target = null )
		{
			if( !tickActions.ContainsKey( name ) )
                tickActions.Add( name, new Pair<MonitorAction, object>( action, target ) );
		}

		public static void DeregisterTickAction( string name )
		{
			tickActions.Remove( name );
		}

        public static void RegisterOccasionalAction( string name, MonitorAction action, object target = null )
		{
            if( !occasionalActions.ContainsKey( name ) ){
                // Invoke accasional actions immediately
                if( action.Invoke( target ) == false )
                    occasionalActions.Add( name, new Pair<MonitorAction, object>( action, target ) );
            }
		}

		public static void DeregisterOccasionalAction( string name )
		{
			occasionalActions.Remove( name );
		}

        public static void RegisterGUIAction( string name, MonitorAction action, object target = null )
        {
			if( !guiActions.ContainsKey( name ) )
                guiActions.Add( name, new Pair<MonitorAction, object>( action, target ) );
        }

        public static void DeregisterGUIAction( string name )
        {
            guiActions.Remove( name );
        }

        public static void RegisterExposeDataAction( string name, MonitorAction action, object target = null )
        {
			if( !exposeActions.ContainsKey( name ) )
                exposeActions.Add( name, new Pair<MonitorAction, object>( action, target ) );
        }

        public static void DeregisterExposeDataAction( string name )
        {
            exposeActions.Remove( name );
        }

        #endregion
    }
}
