using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Ludiq.Commons
{
	public class Registry : Singleton<Registry>
	{
		private Dictionary<Type, IEnumerable> sets = new Dictionary<Type, IEnumerable>();

		public Registry() : base(false, true) { }

		private HashSet<T> GetSet<T>()
		{
			Type type = typeof(T);

			if (sets.ContainsKey(type))
			{
				return (HashSet<T>)sets[type];
			}
			else
			{
				var set = new HashSet<T>();
				sets.Add(type, set);
				return set;
			}
        }

		private void _Register<T>(T item)
		{
			GetSet<T>().Add(item);
		}

		private void _Unregister<T>(T item)
		{
			GetSet<T>().Remove(item);
		}

		private ICollection<T> _Get<T>()
		{
			return GetSet<T>();
		}

		public static void Register<T>(T item)
		{
			instance.GetSet<T>().Add(item);
		}

		public static void Unregister<T>(T item)
		{
			if (instantiated)
			{
				instance.GetSet<T>().Remove(item);
			}
		}

		public static ICollection<T> Get<T>()
		{
			return instance.GetSet<T>();
		}

#if UNITY_EDITOR
		[MenuItem("GameObject/Registry", false, 0)]
		private static void MenuCommand(MenuCommand menuCommand)
		{
			var go = new GameObject("Registry");
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			go.AddComponent<Registry>();
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}
#endif
	}
}