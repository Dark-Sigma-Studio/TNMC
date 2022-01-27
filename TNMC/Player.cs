using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TNMC
{
    public class Player
    {
        public Vector3 pos = Vector3.Zero;
        public Vector3 vel = Vector3.Zero;
        public Vector3 impulse = Vector3.Zero;
        public Vector3 moveimpulse = Vector3.Zero;
        public float eyelevel = 1.5f;

        public double pitch = 0.0;
        public double yaw = 0.0;

        public Matrix3 dirs
        {
            get
            {
                Matrix3 pmat = new Matrix3
                (
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, (float)Math.Cos(pitch), (float)Math.Sin(pitch)),
                new Vector3(0.0f, -(float)Math.Sin(pitch), (float)Math.Cos(pitch))
                );
                Matrix3 ymat = new Matrix3
                    (
                    new Vector3((float)Math.Cos(yaw), (float)Math.Sin(yaw), 0.0f),
                    new Vector3(-(float)Math.Sin(yaw), (float)Math.Cos(yaw), 0.0f),
                    new Vector3(0.0f, 0.0f, 1.0f)
                    );

                return ymat * pmat;
            }
        }

        public Vector3 forward
        {
            get
            {
                return dirs.Column1;
            }
        }
        public Vector3 right
        {
            get
            {
                return dirs.Column0;
            }
        }
        public Vector3 up
        {
            get
            {
                return dirs.Column2;
            }
        }

        public Player()
        {
            MoveState = FlyState;
            CollisionState = NoCollision;
            PhysicsState = FlyPhysics;

            Game.Delegates.Updates += DoPhysics;
            Game.Delegates.Collisions += DoCollisions;
            Game.Delegates.BindUniforms += BindUniforms;
            Game.Delegates.SendUniforms += SendUniforms;
            Game.Delegates.Movement += HandleMovement;
            Game.Delegates.Look += HandleLook;
        }
        #region Uniform stuffs
        int ucampos = -1;
        int ucamdirs = -1;
        public void BindUniforms(int id)
        {
            ucampos = GL.GetUniformLocation(id, "cam.pos");
            ucamdirs = GL.GetUniformLocation(id, "cam.dirs");
        }
        public void SendUniforms()
        {
            Vector3 eyepos = pos + new Vector3(0.0f, 0.0f, eyelevel);
            GL.Uniform3(ucampos, eyepos);

            Matrix3 tdirs = dirs;

            GL.UniformMatrix3(ucamdirs, false, ref tdirs);
        }
        #endregion
        delegate void CollisionDelegate();
        CollisionDelegate CollisionState;
        public void DoCollisions()
        {
            CollisionState();
        }

        public void NoCollision()
        {

        }

        public void NormCollision()
        {
            if(pos.Z < Math.Ceiling((pos.X - 1.0)/ 3.0))
            {
                vel.Z = 0.0f;
                pos.Z = (float)Math.Ceiling((pos.X - 1.0)/ 3.0);
            }
        }

        delegate void PhysicsDelegate(double dt);
        PhysicsDelegate PhysicsState;
        public void DoPhysics(double dt)
        {
            PhysicsState(dt);
        }
        public void NormalPhysics(double dt)
        {
            vel += Game.Gravity * (float)dt * 0.5f + impulse + moveimpulse;
            pos += vel * (float)dt;
            vel += Game.Gravity * (float)dt * 0.5f - moveimpulse;
        }
        public void FlyPhysics(double dt)
        {
            vel += impulse + moveimpulse;
            pos += vel * (float)dt;
            vel *= (float)Math.Pow(1.0 - 0.9, dt);
        }

        delegate void MoveStateDelegate(KeyboardState kstate);
        MoveStateDelegate MoveState;

        public void FlyState(KeyboardState kstate)
        {
            moveimpulse = Vector3.Zero;
            float speed = 3.0f;

            if (kstate.IsKeyDown(Keys.W)) moveimpulse += forward * speed;
            if (kstate.IsKeyDown(Keys.S)) moveimpulse -= forward * speed;
            if (kstate.IsKeyDown(Keys.D)) moveimpulse += right * speed;
            if (kstate.IsKeyDown(Keys.A)) moveimpulse -= right * speed;
        }

        public void WalkState(KeyboardState kstate)
        {
            moveimpulse = Vector3.Zero;
            float speed = 5.0f;

            if (kstate.IsKeyDown(Keys.W)) moveimpulse += (forward - new Vector3(0.0f, 0.0f, forward.Z)).Normalized() * speed;
            if (kstate.IsKeyDown(Keys.S)) moveimpulse -= (forward - new Vector3(0.0f, 0.0f, forward.Z)).Normalized() * speed;
            if (kstate.IsKeyDown(Keys.D)) moveimpulse += right * speed;
            if (kstate.IsKeyDown(Keys.A)) moveimpulse -= right * speed;
        }

        public void HandleMovement(KeyboardState kstate)
        {
            MoveState(kstate);
        }

        public void HandleLook(MouseState mstate, bool iscursorgrabbed)
        {
            Vector2 delta = mstate.Delta;

            if(iscursorgrabbed)
            {
                yaw += delta.X / 1000.0;
                pitch += delta.Y / 1000.0;
                pitch = Math.Clamp(pitch, -1.5, 1.5);
            }
        }
    }
}
