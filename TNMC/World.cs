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
        /* The world is split into two sections; Server and Client.
         * 
         * The server holds references to all active clients.
         * The client holds a reference to the active player.
         * The player holds a reference to the active chunks.
         * 
         * The world client fetches data from the world server
         * The player requests updates from client
         */

        public WorldClient Client;
        public WorldServer Server;


        public World()
        {
            Server = new WorldServer();
            Client = new WorldClient();
        }

        public class WorldServer
        {
            /*  | Fetching data
             *  +- Generation
             *  +- 
             * 
             */
        }

        public class WorldClient
        {
            public Player activePlayer;


        }


        public class MegaChunk
        {
            public uint[,,][,,] Data = new uint[2,2,2][,,];

            public Chunk this[byte cx, byte cy, byte cz]
            {
                get
                {
                    Chunk chunk = new Chunk();
                    chunk.Data = Data[cx, cy, cz];
                    return chunk;
                }
                set
                {
                    Data[cx, cy, cz] = value.Data;
                }
            }
            

            public void Build()
            {
                Chunk chunk = new Chunk();
                chunk.Generate();
                for(byte x = 0; x < 3; x++)
                {
                    for(byte y = 0; y < 3; y++)
                    {
                        for(byte z = 0; z < 3; z++)
                        {
                            Data[x, y, z] = chunk.Data;
                        }
                    }
                }
            }
        }

        public struct Chunk
        {
            public uint[,,] Data = new uint[128,128,128];

            public void Generate()
            {
                for(int z = 0; z < 128; z++)
                {
                    for(int y = 0; y < 128; y++)
                    {
                        for(int x = 0; x < 128; x++)
                        {
                            Data[x,y,z] = (uint)(z <= 0 ? 1 : 0);
                        }
                    }
                }
            }

            public void Bind()
            {
                int id = -1;
                id = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, id);
                GL.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    Data.Length * sizeof(uint),
                    Data,
                    BufferUsageHint.DynamicDraw);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, id);
                //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }

            public void Send()
            {
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)0, (IntPtr)(Data.Length * sizeof(uint)), Data);
            }
        }
    }
}
