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

        public Vector3i selectedcell = new Vector3i();
        public Vector3i buildcell = new Vector3i();
        public bool hasselection = false;

        public double pitch = 0.0;
        public double yaw = 0.0;

        public Game.RenderChunk ActiveChunk;

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

        public Vector3i lastchunk = new Vector3i(0);

        public Vector3i curchunk
        {
            get
            {
                return new Vector3i((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z));
            }
        }

        public Player()
        {
            MoveState = FlyState;
            CollisionState = FlyCollision;
            PhysicsState = FlyPhysics;

            Game.Delegates.Updates += DoPhysics;
            Game.Delegates.Collisions += DoCollisions;
            Game.Delegates.BindUniforms += BindUniforms;
            Game.Delegates.SendUniforms += SendUniforms;
            Game.Delegates.Movement += HandleMovement;
            Game.Delegates.Look += HandleLook;
            Game.Delegates.Look += ConDestruct;


        }
        #region Uniform stuffs
        int ucampos = -1;
        int ucamdirs = -1;
        int uhasselection = -1;
        int uselectedcell = -1;
        public void BindUniforms(int id)
        {
            ucampos = GL.GetUniformLocation(id, "cam.pos");
            ucamdirs = GL.GetUniformLocation(id, "cam.dirs");
            uselectedcell = GL.GetUniformLocation(id, "selectedcell");
            uhasselection = GL.GetUniformLocation(id, "hasselection");
        }
        public void SendUniforms()
        {
            Vector3 eyepos = pos + new Vector3(0.0f, 0.0f, eyelevel);
            GL.Uniform3(ucampos, eyepos);

            Matrix3 tdirs = dirs;

            GL.UniformMatrix3(ucamdirs, false, ref tdirs);

            GL.Uniform1(uhasselection, hasselection ? 1 : 0);
            GL.Uniform3(uselectedcell, selectedcell.X, selectedcell.Y, selectedcell.Z);
        }
        #endregion

        #region Chunk loading stuffs
        public delegate void LoadChunksDelegate(Game.RenderChunk rc, Vector3i location);
        public LoadChunksDelegate LoadChunks;
        public void DoLoadChunks(Vector3i location)
        {
            LoadChunks(ActiveChunk, location);
        }
        #endregion

        delegate void CollisionDelegate();
        CollisionDelegate CollisionState;
        public void DoCollisions()
        {
            CollisionState();
            if(lastchunk != curchunk)
            {
                DoLoadChunks(curchunk);
                lastchunk = curchunk;
            }
        }

        public void NoCollision()
        {

        }

        public void FlyCollision()
        {
            if (float.IsNaN(pos.X) || float.IsNaN(pos.Y) || float.IsNaN(pos.Z)) return;

            double r = 0.5;
            Vector3 cpos = pos + new Vector3(0.0f, 0.0f, eyelevel) + new Vector3(192);

            bool hit = false;
            Vector3 delta = Vector3.Zero;

            Vector3i cell = (Vector3i)cpos;
            Vector3i check = cell;
            for(int z = -1; z <= 1; z++)
            {
                check.Z = cell.Z + z;
                if (check.Z < 0 || check.Z >= 384) continue;
                for(int y = -1; y <= 1; y++)
                {
                    check.Y = cell.Y + y;
                    if (check.Y < 0 || check.Y >= 384) continue;
                    for(int x = -1; x <= 1; x++)
                    {
                        check.X = cell.X + x;
                        if (check.X < 0 || check.X >= 384) continue;

                        if (ActiveChunk[check.X, check.Y, check.Z] == 0) continue;

                        Vector3 nearest = Vector3.Zero;
                        nearest.X = Math.Clamp(cpos.X, check.X, check.X + 1);
                        nearest.Y = Math.Clamp(cpos.Y, check.Y, check.Y + 1);
                        nearest.Z = Math.Clamp(cpos.Z, check.Z, check.Z + 1);

                        Vector3 tonearest = cpos - nearest;
                        if (tonearest.Length >= r || tonearest.Length == 0) continue;

                        Vector3 norm = tonearest.Normalized();
                        Vector3 tpos = norm * (float)r + nearest - new Vector3(192);

                        pos = tpos - new Vector3(0.0f, 0.0f, eyelevel);
                        vel = vel - ((vel * norm) * norm);
                    }
                }
            }
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
            vel = impulse + moveimpulse;
            pos += vel * (float)dt;
            moveimpulse *= (float)Math.Pow(0.1, dt);
        }

        delegate void MoveStateDelegate(KeyboardState kstate);
        MoveStateDelegate MoveState;

        public void FlyState(KeyboardState kstate)
        {
            float speed = 5.0f;

            Vector3 impvect = Vector3.Zero;

            if (kstate.IsKeyDown(Keys.W)) impvect += forward * speed;
            if (kstate.IsKeyDown(Keys.S)) impvect += -forward * speed;
            if (kstate.IsKeyDown(Keys.D)) impvect += right * speed;
            if (kstate.IsKeyDown(Keys.A)) impvect += -right * speed;

            if (impvect.Length > 0) moveimpulse = impvect;
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

                if(mstate.IsButtonDown(MouseButton.Left) && !mstate.WasButtonDown(MouseButton.Left))
                {
                    ActiveChunk[selectedcell.X, selectedcell.Y, selectedcell.Z] = 0;
                }
                if(mstate.IsButtonDown(MouseButton.Right) && !mstate.WasButtonDown(MouseButton.Right))
                {
                    ActiveChunk[buildcell.X, buildcell.Y, buildcell.Z] = 1;
                }
            }
        }

        public void ConDestruct(MouseState mstate, bool iscursorgrabbed)
        {
            //Need to do a DDA test to cast a ray into the scene for voxel ray-cast testing...
            // [Set-up for things we need] //
            Vector3 tonext = Vector3.Zero;
            tonext.X = 1f / Math.Abs(forward.X);
            tonext.Y = 1f / Math.Abs(forward.Y);
            tonext.Z = 1f / Math.Abs(forward.Z);

            Vector3i cellstep = new Vector3i();
            cellstep.X = Math.Sign(forward.X);
            cellstep.Y = Math.Sign(forward.Y);
            cellstep.Z = Math.Sign(forward.Z);
            //===========================================================================//
            // <----- Set-up for first point of intersection -----> //
            Vector3 ro = pos + new Vector3(0f, 0f, eyelevel) + new Vector3(192);

            Vector3i cell = new Vector3i();
            cell.X = (int)Math.Floor(ro.X);
            cell.Y = (int)Math.Floor(ro.Y);
            cell.Z = (int)Math.Floor(ro.Z);

            if (cell.X < 0 || cell.X >= 384 ||
                cell.Y < 0 || cell.Y >= 384 ||
                cell.Z < 0 || cell.Z >= 384) return;

            Vector3i prev = cell;

            Vector3 dists = Vector3.Zero;
            dists.X = ro.X % 1f;
            dists.Y = ro.Y % 1f;
            dists.Z = ro.Z % 1f;

            if (forward.X > 0f) dists.X = 1.0f - dists.X;
            if (forward.Y > 0f) dists.Y = 1.0f - dists.Y;
            if (forward.Z > 0f) dists.Z = 1.0f - dists.Z;

            dists.X /= Math.Abs(forward.X);
            dists.Y /= Math.Abs(forward.Y);
            dists.Z /= Math.Abs(forward.Z);

            float mindist = Math.Min(dists.X, Math.Min(dists.Y, dists.Z));

            Vector3 point = ro + forward * mindist;
            //===========================================================================//
            // <----- DDA core alforithm -----> //
            bool hit = false;
            Vector3 totdists = dists;
            float dist = mindist;
            while(!hit && dist < 6f)
            {
                // [DDA stuffs] //
                mindist = Math.Min(totdists.X, Math.Min(totdists.Y, totdists.Z));
                dist = mindist;

                prev = cell;
                cell += cellstep * new Vector3i(
                    totdists.X == mindist ? 1 : 0,
                    totdists.Y == mindist ? 1 : 0,
                    totdists.Z == mindist ? 1 : 0);

                if (
                    cell.X < 0 || cell.X >= 384 ||
                    cell.Y < 0 || cell.Y >= 384 ||
                    cell.Z < 0 || cell.Z >= 384
                    ) break;

                // [Check the cell] //
                hit = ActiveChunk[cell.X, cell.Y, cell.Z] > 0;
                if (hit) continue;

                //Step forward allong ray] //
                totdists += tonext * new Vector3(
                    totdists.X == mindist ? 1f : 0f,
                    totdists.Y == mindist ? 1f : 0f,
                    totdists.Z == mindist ? 1f : 0f);
            }
            //===========================================================================//
            // <----- Clean-up stuffs -----> //
            point = ro + forward * dist;
            hasselection = hit;
            selectedcell = cell;
            buildcell = prev;
            //===========================================================================//
        }
    }
}
