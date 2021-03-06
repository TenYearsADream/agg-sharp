﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MatterHackers.RenderOpenGl
{
	public struct VertexTextureData
	{
		public float textureU;
		public float textureV;
		public static readonly int Stride = Marshal.SizeOf(default(VertexTextureData));
	}

	public struct VertexColorData
	{
		public byte red;
		public byte green;
		public byte blue;
		public static readonly int Stride = Marshal.SizeOf(default(VertexColorData));
	}

	public struct VertexNormalData
	{
		public float normalX;
		public float normalY;
		public float normalZ;
		public static readonly int Stride = Marshal.SizeOf(default(VertexNormalData));
	}

	public struct VertexPositionData
	{
		public float positionX;
		public float positionY;
		public float positionZ;
		public static readonly int Stride = Marshal.SizeOf(default(VertexPositionData));
	}

	public class SubTriangleMesh
	{
		public ImageBuffer texture = null;
		public VectorPOD<VertexTextureData> textureData = new VectorPOD<VertexTextureData>();
		public VectorPOD<VertexColorData> colorData = new VectorPOD<VertexColorData>();
		public VectorPOD<VertexNormalData> normalData = new VectorPOD<VertexNormalData>();
		public VectorPOD<VertexPositionData> positionData = new VectorPOD<VertexPositionData>();

		public bool UseVertexColors { get; internal set; }
	}

	public class GLMeshTrianglePlugin
	{
		public delegate void DrawToGL(Mesh meshToRender);

		public List<SubTriangleMesh> subMeshs;

		private int meshUpdateCount;

		public static string GLMeshTrianglePluginName => nameof(GLMeshTrianglePluginName);

		static public GLMeshTrianglePlugin Get(Mesh mesh, Func<Vector3, Color> getColorFunc = null)
		{
			object meshData;
			mesh.PropertyBag.TryGetValue(GLMeshTrianglePluginName, out meshData);
			if (meshData is GLMeshTrianglePlugin plugin)
			{
				if (mesh.ChangedCount == plugin.meshUpdateCount)
				{
					return plugin;
				}

				// else we need to rebuild the data
				plugin.meshUpdateCount = mesh.ChangedCount;
				mesh.PropertyBag.Remove(GLMeshTrianglePluginName);
			}

			GLMeshTrianglePlugin newPlugin = new GLMeshTrianglePlugin();
			newPlugin.CreateRenderData(mesh, getColorFunc);
			newPlugin.meshUpdateCount = mesh.ChangedCount;
			mesh.PropertyBag.Add(GLMeshTrianglePluginName, newPlugin);

			return newPlugin;
		}

		private GLMeshTrianglePlugin()
		{
			// This is private as you can't build one of these. You have to call GetImageGLDisplayListPlugin.
		}

		private void CreateRenderData(Mesh meshToBuildListFor, Func<Vector3, Color> getColorFunc)
		{
			subMeshs = new List<SubTriangleMesh>();
			SubTriangleMesh currentSubMesh = null;
			VectorPOD<VertexTextureData> textureData = null;
			VectorPOD<VertexColorData> colorData = null;
			VectorPOD<VertexNormalData> normalData = null;
			VectorPOD<VertexPositionData> positionData = null;
			// first make sure all the textures are created
			foreach (Face face in meshToBuildListFor.Faces)
			{
				ImageBuffer faceTexture = face.GetTexture(0);
				if (faceTexture != null)
				{
					ImageGlPlugin.GetImageGlPlugin(faceTexture, true);
				}

				// don't compare the data of the texture but rather if they are just the same object
				if (subMeshs.Count == 0 || (object)subMeshs[subMeshs.Count - 1].texture != (object)faceTexture)
				{
					SubTriangleMesh newSubMesh = new SubTriangleMesh();
					newSubMesh.texture = faceTexture;
					subMeshs.Add(newSubMesh);
					if (getColorFunc != null)
					{
						newSubMesh.UseVertexColors = true;
					}

					currentSubMesh = subMeshs[subMeshs.Count - 1];
					textureData = currentSubMesh.textureData;
					colorData = currentSubMesh.colorData;
					normalData = currentSubMesh.normalData;
					positionData = currentSubMesh.positionData;
				}

				Vector2[] textureUV = new Vector2[2];
				Vector3[] position = new Vector3[2];
				VertexColorData color = new VertexColorData();

				if (getColorFunc != null)
				{
					var faceColor = getColorFunc(face.Normal);
					color = new VertexColorData
					{
						red = faceColor.red,
						green = faceColor.green,
						blue = faceColor.blue
					};
				}

				int vertexIndex = 0;
				foreach (FaceEdge faceEdge in face.FaceEdges())
				{
					if (vertexIndex < 2)
					{
						textureUV[vertexIndex] = faceEdge.GetUv(0);
						position[vertexIndex] = faceEdge.FirstVertex.Position;
					}
					else
					{
						VertexTextureData tempTexture;
						VertexNormalData tempNormal;
						VertexPositionData tempPosition;
						tempTexture.textureU = (float)textureUV[0].X; tempTexture.textureV = (float)textureUV[0].Y;
						tempNormal.normalX = (float)face.Normal.X; tempNormal.normalY = (float)face.Normal.Y; tempNormal.normalZ = (float)face.Normal.Z;
						tempPosition.positionX = (float)position[0].X; tempPosition.positionY = (float)position[0].Y; tempPosition.positionZ = (float)position[0].Z;
						textureData.Add(tempTexture);
						normalData.Add(tempNormal);
						positionData.Add(tempPosition);
						colorData.add(color);

						tempTexture.textureU = (float)textureUV[1].X; tempTexture.textureV = (float)textureUV[1].Y;
						tempNormal.normalX = (float)face.Normal.X; tempNormal.normalY = (float)face.Normal.Y; tempNormal.normalZ = (float)face.Normal.Z;
						tempPosition.positionX = (float)position[1].X; tempPosition.positionY = (float)position[1].Y; tempPosition.positionZ = (float)position[1].Z;
						textureData.Add(tempTexture);
						normalData.Add(tempNormal);
						positionData.Add(tempPosition);
						colorData.add(color);

						Vector2 textureUV2 = faceEdge.GetUv(0);
						Vector3 position2 = faceEdge.FirstVertex.Position;
						tempTexture.textureU = (float)textureUV2.X; tempTexture.textureV = (float)textureUV2.Y;
						tempNormal.normalX = (float)face.Normal.X; tempNormal.normalY = (float)face.Normal.Y; tempNormal.normalZ = (float)face.Normal.Z;
						tempPosition.positionX = (float)position2.X; tempPosition.positionY = (float)position2.Y; tempPosition.positionZ = (float)position2.Z;
						textureData.Add(tempTexture);
						normalData.Add(tempNormal);
						positionData.Add(tempPosition);
						colorData.add(color);

						textureUV[1] = faceEdge.GetUv(0);
						position[1] = faceEdge.FirstVertex.Position;
					}

					vertexIndex++;
				}
			}
		}

		public void Render()
		{
		}

		public static void AssertDebugNotDefined()
		{
#if DEBUG
			throw new Exception("DEBUG is defined and should not be!");
#endif
		}
	}
}