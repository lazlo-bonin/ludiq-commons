using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ludiq.Commons
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class EditorCamera : MonoBehaviour
	{
#if UNITY_EDITOR
		Camera editorCamera;

		void Update()
		{
			if (editorCamera == null)
			{
				editorCamera = EditorWindow.GetWindow<SceneView>().camera;
			}

			if (editorCamera == null)
			{
				return;
			}

			var selfCamera = GetComponent<Camera>();
			var selfComponents = GetComponents<Component>();
			var editorComponents = editorCamera.GetComponents<Component>();
			var selfComponentTypes = new List<Type>();

			// Copy components to editor
			foreach (var selfComponent in selfComponents)
			{
				var selfComponentType = selfComponent.GetType();
				selfComponentTypes.Add(selfComponentType);

				if (selfComponent is EditorCamera ||
					selfComponent is Transform)
				{
					continue;
				}

				var cameraComponent = editorCamera.GetComponent(selfComponentType) ?? editorCamera.gameObject.AddComponent(selfComponentType);

				if (cameraComponent != null)
				{
                    EditorUtility.CopySerialized(selfComponent, cameraComponent);
				}
			}

			// Remove old components from editor
			foreach (var editorComponent in editorComponents)
			{
				var editorComponentType = editorComponent.GetType();

				if (editorComponentType == typeof(Behaviour) ||
					editorComponentType == typeof(FlareLayer))
				{
					continue;
				}

				if (!selfComponentTypes.Contains(editorComponentType))
				{
					DestroyImmediate(editorComponent);
				}
			}

			// Keep the program buffers in sync
			editorCamera.RemoveAllCommandBuffers();

			foreach (CameraEvent cameraEvent in Enum.GetValues(typeof(CameraEvent)))
			{
				foreach (var selfCommandBuffer in selfCamera.GetCommandBuffers(cameraEvent))
				{
					editorCamera.AddCommandBuffer(cameraEvent, selfCommandBuffer);
	            }
			}

			EditorUtility.CopySerialized(editorCamera, selfCamera);
		}

		[MenuItem("GameObject/Editor Camera", false, 11)]
		private static void MenuCommand(MenuCommand menuCommand)
		{
			var go = new GameObject("Editor Camera");
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			go.AddComponent<EditorCamera>();
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}
#endif
	}
}