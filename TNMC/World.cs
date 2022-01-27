using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace TNMC
{
    public class World
    {
        public struct Chunk
        {
            public uint[,,] Data = new uint[16,16,16];

            public void Generate()
            {
                for(int z = 0; z < 16; z++)
                {
                    for(int y = 0; y < 16; y++)
                    {
                        for(int x = 0; x < 16; x++)
                        {
                            Data[x,y,z] = 1;
                        }
                    }
                }
            }

            public void Send()
            {
                int id = -1;
                id = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, id);
                GL.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    Data.Length * sizeof(uint),
                    Data,
                    BufferUsageHint.DynamicDraw);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, id);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }
        }
    }
}
