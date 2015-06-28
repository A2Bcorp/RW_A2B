using System;
using System.Collections.Generic;
using Verse;

namespace A2B
{
    public class A2BMonitor : MapComponent
    {
        private static Dictionary<string, Action> tickActions     = new Dictionary<string, Action>();
        private static Dictionary<string, Action> updateActions   = new Dictionary<string, Action>();
        private static Dictionary<string, Action> guiActions      = new Dictionary<string, Action>();
        private static Dictionary<string, Action> exposeActions   = new Dictionary<string, Action>();

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            foreach (var p in updateActions) {
                p.Value();
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            foreach (var p in tickActions) {
                p.Value();
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            foreach (var p in guiActions) {
                p.Value();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            foreach (var p in exposeActions) {
                p.Value();
            }
        }

        #region Register/Deregister Actions

        public static void RegisterUpdateAction(string name, Action action)
        {
            updateActions.Add(name, action);
        }

        public static void DeregisterUpdateAction(string name)
        {
            updateActions.Remove(name);
        }

        public static void RegisterTickAction(string name, Action action)
        {
            tickActions.Add(name, action);
        }

        public static void DeregisterTickAction(string name)
        {
            tickActions.Remove(name);
        }

        public static void RegisterGUIAction(string name, Action action)
        {
            guiActions.Add(name, action);
        }

        public static void DeregisterGUIAction(string name)
        {
            guiActions.Remove(name);
        }

        public static void RegisterExposeDataAction(string name, Action action)
        {
            exposeActions.Add(name, action);
        }

        public static void DeregisterExposeDataAction(string name)
        {
            exposeActions.Remove(name);
        }

        #endregion
    }
}
