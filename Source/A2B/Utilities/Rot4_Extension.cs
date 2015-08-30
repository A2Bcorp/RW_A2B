using System;
using RimWorld;
using Verse;

namespace A2B
{

    public static class Rot4_Extension
    {
        public static string        Name( this Rot4 r )
        {
            if( r == Rot4.North )
                return Constants.TxtDirectionNorth.Translate();
            if( r == Rot4.East )
                return Constants.TxtDirectionEast.Translate();
            if( r == Rot4.South )
                return Constants.TxtDirectionSouth.Translate();
            if( r == Rot4.West )
                return Constants.TxtDirectionWest.Translate();
            if( r == Rot4.Invalid )
                return "...";

            return "Unknown (" + r.ToString() + ")";
        }

        public static Rot4          OppositeOf( this Rot4 r )
        {
            return new Rot4( ( r.AsInt + 2 ) % 4 );
        }

        public static Rot4          LeftOf( this Rot4 r )
        {
            return new Rot4( ( r.AsInt + 3 ) % 4 );
        }

        public static Rot4          RightOf( this Rot4 r )
        {
            return new Rot4( ( r.AsInt + 1 ) % 4 );
        }

    }
}

