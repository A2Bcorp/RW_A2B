#region Usings

using System;
using System.Collections.Generic;
using Verse;

#endregion

namespace A2B
{
    public static class Scribe_Fixed
    {
        public static void LookDictionary<K, V>(ref Dictionary<K, V> dict, string dictLabel, LookMode keyLookMode = LookMode.Undefined,
            LookMode valueLookMode = LookMode.Undefined)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                return;
            }

            Scribe.EnterNode(dictLabel);

            var list1 = new List<K>();
            var list2 = new List<V>();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (dict == null)
                {
                    throw new ArgumentNullException("dict");
                }

                foreach (var v in dict)
                {
                    list1.Add(v.Key);
                    list2.Add(v.Value);
                }
            }

            Scribe_Collections.LookList(ref list1, "keys", keyLookMode);
            Scribe_Collections.LookList(ref list2, "values", valueLookMode);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (dict == null)
                {
                    dict = new Dictionary<K, V>();
                }
                else
                {
                    dict.Clear();
                }

                if (list1 != null && list2 != null)
                {
                    for (var index = 0; index < list1.Count; ++index)
                    {
                        dict.Add(list1[index], list2[index]);
                    }
                }
            }

            Scribe.ExitNode();
        }
    }
}
