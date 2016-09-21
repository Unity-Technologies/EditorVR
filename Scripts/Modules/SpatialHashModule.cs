﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Data;

namespace UnityEngine.VR.Modules
{
	public class SpatialHashModule : MonoBehaviour
	{
		private SpatialHash<Renderer> m_SpatialHash;

		public bool showGizmos { get; set; }

		internal void Setup(SpatialHash<Renderer> hash)
		{
			m_SpatialHash = hash;
			SetupObjects();
			StartCoroutine(UpdateDynamicObjects());
		}

		void SetupObjects()
		{
			MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
			foreach (var mf in meshFilters)
			{
				if (mf.sharedMesh)
				{
					// Exclude EditorVR objects
					if (mf.GetComponentInParent<EditorVR>())
						continue;

					Renderer renderer = mf.GetComponent<Renderer>();
					if (renderer)
						m_SpatialHash.AddObject(renderer, renderer.bounds);
				}
			}
		}

		private void OnDrawGizmos()
		{
			if (m_SpatialHash != null && showGizmos)
				m_SpatialHash.DrawGizmos();
		}

		private IEnumerator UpdateDynamicObjects()
		{
			while (true)
			{
				// TODO AE 9/21/16: Hook updates of new objects that are created
				List<Renderer> allObjects = new List<Renderer>(m_SpatialHash.allObjects);
				foreach (var obj in allObjects)
				{
					if (obj.transform.hasChanged)
					{
						m_SpatialHash.RemoveObject(obj);
						m_SpatialHash.AddObject(obj, obj.bounds);
						obj.transform.hasChanged = false;
					}
				}

				yield return null;
			}
		}
	}
}