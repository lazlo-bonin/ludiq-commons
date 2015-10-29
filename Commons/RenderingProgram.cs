using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Illusoire
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public abstract class RenderingProgram : MonoBehaviour
	{
		public bool debug;
		private new Camera camera;
		protected CameraEvent hook { get; private set; }
		protected string programName { get; private set; }

		protected RenderingProgram(CameraEvent hook)
		{
			this.hook = hook;
		}

		protected virtual void Awake()
		{
			programName = GetType().Name;
			camera = GetComponent<Camera>();
			Attach();
		}

		protected abstract void Program(CommandBuffer buffer, Camera camera);

		public void Invalidate()
		{
			if (enabled)
			{
				if (debug)
				{
					Debug.LogWarningFormat("{0}.{1}: Invalidating.\n", camera.name, programName);
				}

				Attach();
			}
		}

		protected virtual void OnEnable()
		{
			Attach();
		}

		protected virtual void OnDisable()
		{
			Detach();
		}
		
		private CommandBuffer Attach(bool safetyDetach = true)
		{
			if (safetyDetach)
			{
				Detach();
			}

			var buffer = new CommandBuffer();
			buffer.name = programName;
			Program(buffer, camera);
			camera.AddCommandBuffer(hook, buffer);

			if (debug)
			{
				Debug.LogFormat("{0}.{1}: Added buffer.\n", camera.name, programName);
			}
			
			return buffer;
		}

		private CommandBuffer Get()
		{
			return camera.GetCommandBuffers(hook).FirstOrDefault(b => b.name == programName);
		}

		private bool Detach()
		{
			var buffer = Get();

			if (buffer != null)
			{
				camera.RemoveCommandBuffer(hook, buffer);

				if (debug)
				{
					Debug.LogFormat("{0}.{1}: Removed buffer.\n", camera.name, programName);
				}

				return true;
			}

			return false;
		}
	}
}