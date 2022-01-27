using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Platform;
using OpenTK.Windowing;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace TNMC
{
    public class Game : GameWindow
    {
        #region Shader things

        public struct Shader
        {
            public int id;

            public static Shader Load(string location, ShaderType type)
            {
                // Declarations
                int id = GL.CreateShader(type);

                // Processing
                GL.ShaderSource(id, File.ReadAllText(location));
                GL.CompileShader(id);

                // Error checking
                string infolog = GL.GetShaderInfoLog(id);
                if (!string.IsNullOrEmpty(infolog))
                {
                    throw new Exception(infolog);
                }

                // Return
                return new Shader() { id = id };
            }
        }

        public struct ShaderProgram
        {
            #region Fields
            public int id;
            #endregion

            #region Methods
            public static ShaderProgram Load(string vertlocation, string fraglocation)
            {
                // Declarations
                int id = GL.CreateProgram();

                Shader vert = Shader.Load(vertlocation, ShaderType.VertexShader);
                Shader frag = Shader.Load(fraglocation, ShaderType.FragmentShader);

                // Processing
                GL.AttachShader(id, vert.id);
                GL.AttachShader(id, frag.id);
                GL.LinkProgram(id);

                // Cleanup
                GL.DetachShader(id, vert.id);
                GL.DetachShader(id, frag.id);
                GL.DeleteShader(vert.id);
                GL.DeleteShader(frag.id);

                // Error checking
                string infolog = GL.GetProgramInfoLog(id);
                if (!string.IsNullOrEmpty(infolog))
                {
                    throw new Exception(infolog);
                }

                // Retern
                return new ShaderProgram() { id = id };
            }
            #endregion
        }

        #endregion
        #region Boilerplate screen stuffs
        private int vbo;
        private int vao;
        public static ShaderProgram sprog = new ShaderProgram() { id = 0 };
        private int uiResolution = -1;
        private readonly float[] verteces =
        {
            -1.0f, -1.0f, 0.0f,
            -1.0f, 3.0f, 0.0f,
            3.0f, -1.0f, 0.0f
        };
        #endregion
        #region Delegate Stuffs
        public static class Delegates
        {
            #region Delegate Declarations
            public delegate void TimeSensitiveDelegate(double deltat);
            public delegate void ShaderProgramDelegate(int sprogid);
            public delegate void StaticDelegate();
            public delegate void KeyboardDelegate(KeyboardState keyboardstate);
            public delegate void MouseDelegate(MouseState mousestate, bool iscursorgrabbed);
            #endregion
            #region Delegates
            public static TimeSensitiveDelegate? Updates;
            public static StaticDelegate? Collisions;
            public static StaticDelegate? SendUniforms;
            public static ShaderProgramDelegate? BindUniforms;
            public static StaticDelegate? BindSSBO;
            public static KeyboardDelegate? Movement;
            public static MouseDelegate? Look;

            #endregion
        }
        #endregion
        #region Time class
        public static class Time
        {
            public static Stopwatch sw = new Stopwatch();
            public static double ellapsed;
            public static double deltatime;
        }
        #endregion
        #region Global stuffs
        public static Vector3 Gravity = new Vector3(0.0f, 0.0f, -10.0f);
        public Player player = new Player();
        public static Random random = new Random();
        #endregion

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        public World.Chunk tchunk = new World.Chunk();

        protected override void OnLoad()
        {
            base.OnLoad();

            tchunk.Generate();

            GL.ClearColor(0.125f, 0.175f, 0.257f, 1.0f);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verteces.Length * sizeof(float), verteces, BufferUsageHint.StaticDraw);

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            sprog = ShaderProgram.Load("Resources/Screen.vert", "Resources/Screen.frag");
            GL.UseProgram(sprog.id);

            tchunk.Send();

            uiResolution = GL.GetUniformLocation(sprog.id, "iResolution");
            if (Delegates.BindUniforms != null) Delegates.BindUniforms(sprog.id);

            Time.sw.Start();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        //bool Cursor = true;

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            #region Boilerplate
            base.OnUpdateFrame(args);
            //-------------------------------------//
            var input = KeyboardState;

            if (input.IsKeyPressed(Keys.Escape))
            {
                CursorGrabbed = !CursorGrabbed;
                if (!CursorGrabbed)
                {
                    CursorVisible = true;
                    MousePosition = Size / 2;
                }
            }

            Time.deltatime = Time.sw.ElapsedMilliseconds / 1000.0 - Time.ellapsed;
            Time.ellapsed += Time.deltatime;
            #endregion
            #region Delegate handling
            if (Delegates.Movement != null)     Delegates.Movement(KeyboardState);
            if (Delegates.Look != null)         Delegates.Look(MouseState, CursorGrabbed);
            if (Delegates.Updates != null)      Delegates.Updates(Time.deltatime);
            if (Delegates.Collisions != null)   Delegates.Collisions();
            #endregion
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            #region Boilerplate
            base.OnRenderFrame(args);
            //---------------------------------------//
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            #endregion
            //=============================================================================//
            GL.UseProgram(sprog.id);

            GL.Uniform2(uiResolution, Size);
            if(Delegates.SendUniforms != null) Delegates.SendUniforms();

            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            //=============================================================================//
            SwapBuffers();
        }
    }
}
