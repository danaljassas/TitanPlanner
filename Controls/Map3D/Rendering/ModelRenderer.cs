using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.ES20;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// Draws the vehicle STL. The mesh is uploaded once (positions + normals) and lit in the shader, rather
    /// than re-shading and re-uploading every vertex each frame.
    /// </summary>
    public sealed class ModelRenderer : IDisposable
    {
        const float LightX = 0.4f, LightY = 0.6f, LightZ = 0.7f;

        readonly STLModelLoader _loader;
        GLProgram _program;
        int _vbo;
        int _vertexCount;
        List<float> _uploadedVertices;
        string _failedKey;
        bool _hasMaterialColors;

        public ModelRenderer(STLModelLoader loader)
        {
            _loader = loader;
        }

        /// <summary>Draws the model with the given model matrix (scale, rotation and translation, row-vector order).</summary>
        public void Draw(ref Matrix4 projection, ref Matrix4 view, Matrix4 model, Color color)
        {
            if (!EnsureUploaded())
                return;

            if (_program == null)
                _program = GLProgram.Create(Shaders.LitVertex, Shaders.LitFragment);

            var modelView = Matrix4.Mult(model, view);
            float lightLength = (float)Math.Sqrt(LightX * LightX + LightY * LightY + LightZ * LightZ);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.Disable(EnableCap.CullFace);

            _program.Use();
            _program.SetMatrix("Projection", ref projection);
            _program.SetMatrix("ModelView", ref modelView);
            _program.SetMatrix("Model", ref model);
            _program.SetVector3("LightDirection", LightX / lightLength, LightY / lightLength, LightZ / lightLength);
            _program.SetColor("BaseColor", _hasMaterialColors ? 1 : color.R / 255f,
                _hasMaterialColors ? 1 : color.G / 255f, _hasMaterialColors ? 1 : color.B / 255f, 1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            _program.Pointer("Position", 3, 9, 0);
            _program.Pointer("Normal", 3, 9, 3);
            _program.Pointer("MaterialColor", 3, 9, 6);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
            _program.DisablePointer("Position");
            _program.DisablePointer("Normal");
            _program.DisablePointer("MaterialColor");
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        bool EnsureUploaded()
        {
            // STLModelLoader reports load errors with a message box; don't retry a failed model every frame.
            string key = _loader.CustomSTLPath + "|" + (MainV2.comPort?.MAV?.aptype.ToString() ?? "");
            if (key == _failedKey)
                return false;

            if (!_loader.EnsureLoaded() || _loader.Vertices == null || _loader.VertexCount == 0)
            {
                _failedKey = key;
                return false;
            }

            _failedKey = null;
            if (ReferenceEquals(_loader.Vertices, _uploadedVertices) && _vbo != 0)
                return true;

            var vertices = _loader.Vertices;
            var normals = _loader.Normals;
            int count = vertices.Count / 3;
            var colors = _loader.TriangleColors;
            _hasMaterialColors = colors != null;
            var interleaved = new float[count * 9];
            for (int i = 0; i < count; i++)
            {
                interleaved[i * 9] = vertices[i * 3];
                interleaved[i * 9 + 1] = vertices[i * 3 + 1];
                interleaved[i * 9 + 2] = vertices[i * 3 + 2];
                interleaved[i * 9 + 3] = normals[i * 3];
                interleaved[i * 9 + 4] = normals[i * 3 + 1];
                interleaved[i * 9 + 5] = normals[i * 3 + 2];
                int triangleColor = i / 3 * 3;
                interleaved[i * 9 + 6] = colors == null ? 1 : colors[triangleColor] / 255f;
                interleaved[i * 9 + 7] = colors == null ? 1 : colors[triangleColor + 1] / 255f;
                interleaved[i * 9 + 8] = colors == null ? 1 : colors[triangleColor + 2] / 255f;
            }

            if (_vbo == 0)
                GL.GenBuffers(1, out _vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, interleaved.Length * sizeof(float), interleaved,
                BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            _vertexCount = count;
            _uploadedVertices = vertices;
            return true;
        }

        public void Dispose()
        {
            if (_vbo != 0)
            {
                GL.DeleteBuffers(1, ref _vbo);
                _vbo = 0;
            }

            _program?.Dispose();
        }
    }
}
