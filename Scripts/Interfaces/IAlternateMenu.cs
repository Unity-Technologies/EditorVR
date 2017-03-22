﻿#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// An alternate menu that shows on device proxies
	/// </summary>
	public interface IAlternateMenu : IMenu, IUsesMenuActions, IUsesRayOrigin
	{
		/// <summary>
		/// Delegate called when any item was selected in the alternate menu
		/// </summary>
		event Action<Transform> itemWasSelected;

		/// <summary>
		/// If true the menu will maintain fully-opaque visibilty for a period of time when revealed
		/// Used when unlocking objects, in order to maintain full menu opacity for a period of time, drawing attention to the menu
		/// </summary>
		bool opaqueReveal { set; }
	}
}
#endif
