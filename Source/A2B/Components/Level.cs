using System;

namespace A2B
{
	[Flags]
	public enum Level
	{
		Surface = 1,
		Underground = 2,
		Both = Surface | Underground
	}
}