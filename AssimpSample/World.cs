﻿// -----------------------------------------------------------------------
// <file>World.cs</file>
// <copyright>Grupa za Grafiku, Interakciju i Multimediju 2013.</copyright>
// <author>Srđan Mihić</author>
// <author>Aleksandar Josić</author>
// <summary>Klasa koja enkapsulira OpenGL programski kod.</summary>
// -----------------------------------------------------------------------
using System;
using SharpGL.SceneGraph.Quadrics;
using SharpGL;
using SharpGL.SceneGraph.Cameras;
using System.Windows.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace AssimpSample
{
    // Klasa enkapsulira OpenGL kod i omogucava njegovo iscrtavanje i azuriranje.
    public class World : IDisposable
    {
        #region Atributi

        // Atribut kamere
        private LookAtCamera lookAtCam;

        // Atributi za animaciju
        private DispatcherTimer timer1;
        private DispatcherTimer timer2;
        private float golfClubAngle = 0f;
        private bool golfClubGoingUp = true;
        private float ballPosition_x_z = 0f;
        private float ballPosition_y = 0f;
        private bool animationActive = false;

        // Teksture
        private enum TextureObjects { Grass = 0, YellowPlastic, GolfBall };
        private readonly int m_textureCount = Enum.GetNames(typeof(TextureObjects)).Length;
        private uint[] m_textures = null;
        private string[] m_textureFiles = { "..//..//images//grass1.jpg", "..//..//images//plastic.jpg", "..//..//images//golfBall.jpg" };

        // Scena koja se prikazuje.
        private AssimpScene m_golf_club;

        // Ugao rotacije sveta oko X ose.
        private float m_xRotation = 5f;

        // Ugao rotacije sveta oko Y ose.
        private float m_yRotation = 45f;

        // Udaljenost scene od kamere.
        private float m_sceneDistance = 80f;

        // Transliranje po X osi preko tastature
        private float x_translate = 0.0f;

        // Transliranje po Y osi preko tastature
        private float y_translate = 0.0f;

        // Sirina OpenGL kontrole u pikselima.
        private int m_width;

        // Visina OpenGL kontrole u pikselima.
        private int m_height;

        #endregion Atributi

        #region Properties

        public LookAtCamera LookAtCam
        {
            get { return lookAtCam; }
            set { lookAtCam = value; }
        }

        public float GolfClubAngle
        {
            get { return golfClubAngle; }
            set { golfClubAngle = value; }
        }

        public float BallPosition_x_z
        {
            get { return ballPosition_x_z; }
            set { ballPosition_x_z = value; }
        }

        public float BallPosition_y
        {
            get { return ballPosition_y; }
            set { ballPosition_y = value; }
        }

        public bool AnimationActive
        {
            get { return animationActive; }
            set { animationActive = value; }
        }

        public AssimpScene Scene
        {
            get { return m_golf_club; }
            set { m_golf_club = value; }
        }

        public float RotationX
        {
            get { return m_xRotation; }
            set { m_xRotation = value; }
        }

        public float RotationY
        {
            get { return m_yRotation; }
            set { m_yRotation = value; }
        }

        public float SceneDistance
        {
            get { return m_sceneDistance; }
            set { m_sceneDistance = value; }
        }

        public int Width
        {
            get { return m_width; }
            set { m_width = value; }
        }

        public int Height
        {
            get { return m_height; }
            set { m_height = value; }
        }

        public float X_translate
        {
            get { return x_translate; }
            set { x_translate = value; }
        }

        public float Y_translate
        {
            get { return y_translate; }
            set { y_translate = value; }
        }

        #endregion Properties

        #region Konstruktori

        public World(String scenePath, String sceneFileName, int width, int height, OpenGL gl)
        {
            this.m_golf_club = new AssimpScene(scenePath, sceneFileName, gl);
            this.m_width = width;
            this.m_height = height;
            m_textures = new uint[m_textureCount];
        }

        ~World()
        {
            this.Dispose(false);
        }

        #endregion Konstruktori

        #region Metode

        // Korisnicka inicijalizacija i podesavanje OpenGL parametara.
        public void Initialize(OpenGL gl)
        {
            // Model sencenja na flat (konstantno)
            gl.ShadeModel(OpenGL.GL_FLAT);

            // 1.1 Testiranje dubine i sakrivanje nevidljivih povrsina
            DepthTestingAndFaceCulling(gl);

            // 2.1 Color Tracking mehanizam
            ColorTrackingMechanism(gl);

            // 3D modeli
            Initialize3DModels();

            // Podesavanje inicijalnih parametara kamere
            /*lookAtCam = new LookAtCamera();
            lookAtCam.Position = new Vertex(0f, 0f, 0f);
            lookAtCam.Target = new Vertex(0f, 0f, -10f);
            lookAtCam.UpVector = new Vertex(0f, 1f, 0f);
            lookAtCam.Project(gl);*/

            // Definisanje tajmera za animaciju
            CreateTimers();

            // Teksture
            CreateTextures(gl);
        }

        private static void DepthTestingAndFaceCulling(OpenGL gl)
        {
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.CullFace(OpenGL.GL_BACK);
            gl.Enable(OpenGL.GL_CULL_FACE);
        }

        private static void ColorTrackingMechanism(OpenGL gl)
        {
            gl.ColorMaterial(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT_AND_DIFFUSE);
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);

            gl.ClearColor(0.65f, 0.92f, 0.98f, 1.0f);
        }

        private void Initialize3DModels()
        {
            m_golf_club.LoadScene();
            m_golf_club.Initialize();
        }

        private void CreateTimers()
        {
            timer1 = new DispatcherTimer();
            timer1.Interval = TimeSpan.FromMilliseconds(10);
            timer1.Tick += new EventHandler(RunGolfClubAnimation);

            timer2 = new DispatcherTimer();
            timer2.Interval = TimeSpan.FromMilliseconds(7);
            timer2.Tick += new EventHandler(RunBallAnimation);
        }

        private void CreateTextures(OpenGL gl)
        {
            gl.Enable(OpenGL.GL_TEXTURE_2D);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_REPLACE);

            gl.GenTextures(m_textureCount, m_textures);
            for (int i = 0; i < m_textureCount; ++i)
            {
                // Pridruzi teksturu odgovarajucem identifikatoru
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[i]);

                // Ucitaj sliku i podesi parametre teksture
                Bitmap image = new Bitmap(m_textureFiles[i]);
                // rotiramo sliku zbog koordinantog sistema opengl-a
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                // RGBA format (dozvoljena providnost slike tj. alfa kanal)
                BitmapData imageData = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                      System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                gl.Build2DMipmaps(OpenGL.GL_TEXTURE_2D, (int)OpenGL.GL_RGBA8, image.Width, image.Height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, imageData.Scan0);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_REPEAT);		// Wrapping - GL_REPEAT - S
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_REPEAT);		// Wrapping - GL_REPEAT - T
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);		// Nearest Filtering
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);		// Nearest Filtering

                image.UnlockBits(imageData);
                image.Dispose();
            }
        }

        // Iscrtavanje OpenGL kontrole.
        public void Draw(OpenGL gl)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            //lookAtCam.Project(gl);

            float ground_size = 35f;

            gl.PushMatrix();
            gl.Translate(0f, 0f, -m_sceneDistance);
            gl.Rotate(m_xRotation, 1.0f, 0.0f, 0.0f);
            gl.Rotate(m_yRotation, 0.0f, 1.0f, 0.0f);

                #region Ground
                gl.PushMatrix();
                gl.Translate(0f, -10f, 0f);

                    #region Hole
                    gl.PushMatrix();
                    gl.Translate(20f, 0.01f, -20f);
                    gl.Rotate(-90f, 1f, 0f, 0f);
                    gl.Color(0.19f, 0.15f, 0.11f);
                    Disk disk = new Disk();
                    disk.Slices = 40;
                    disk.InnerRadius = 0f;
                    disk.OuterRadius = 2.5f;
                    disk.CreateInContext(gl);
                    disk.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);

                    gl.Translate(0f, 0f, 0.1f);
                    gl.Color(1f, 1f, 1f);
                    disk.Slices = 40;
                    disk.InnerRadius = 0f;
                    disk.OuterRadius = 1.8f;
                    disk.CreateInContext(gl);
                    disk.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
                    gl.PopMatrix();
                    #endregion

                #region Grass
                gl.MatrixMode(OpenGL.GL_TEXTURE);

                    gl.PushMatrix();
                    gl.Scale(1.1f, 1.1f, 1.1f);
                    gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Grass]);
                    gl.Begin(OpenGL.GL_QUADS);
                    gl.Color(0.19f, 0.85f, 0.26f);
                    gl.TexCoord(0.0f, 0.0f);
                    gl.Vertex(ground_size, 0f, ground_size);
                    gl.TexCoord(0.0f, 1.0f);
                    gl.Vertex(ground_size, 0f, -ground_size);
                    gl.TexCoord(1.0f, 1.0f);
                    gl.Vertex(-ground_size, 0f, -ground_size);
                    gl.TexCoord(1.0f, 0.0f);
                    gl.Vertex(-ground_size, 0f, ground_size);
                    gl.End();
                    gl.PopMatrix();

                gl.MatrixMode(OpenGL.GL_MODELVIEW);

                gl.Begin(OpenGL.GL_QUADS);
                gl.Color(0.19f, 0.15f, 0.11f);
                gl.Vertex(ground_size, 0f, ground_size);
                gl.Vertex(ground_size, -3f, ground_size);
                gl.Vertex(ground_size, -3f, -ground_size);
                gl.Vertex(ground_size, 0f, -ground_size);

                gl.Vertex(ground_size, 0f, ground_size);
                gl.Vertex(-ground_size, 0f, ground_size);
                gl.Vertex(-ground_size, -3f, ground_size);
                gl.Vertex(ground_size, -3f, ground_size);

                gl.Vertex(-ground_size, 0f, ground_size);
                gl.Vertex(-ground_size, 0f, -ground_size);
                gl.Vertex(-ground_size, -3f, -ground_size);
                gl.Vertex(-ground_size, -3f, ground_size);

                gl.Vertex(-ground_size, 0f, -ground_size);
                gl.Vertex(ground_size, 0f, -ground_size);
                gl.Vertex(ground_size, -3f, -ground_size);
                gl.Vertex(-ground_size, -3f, -ground_size);

                gl.Vertex(ground_size, -3f, ground_size);
                gl.Vertex(-ground_size, -3f, ground_size);
                gl.Vertex(-ground_size, -3f, -ground_size);
                gl.Vertex(ground_size, -3f, -ground_size);
                gl.End();
                #endregion

                    #region Flag Stick
                    gl.Disable(OpenGL.GL_CULL_FACE);
                    gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.YellowPlastic]);
                    gl.PushMatrix();
                    gl.Translate(20f, 0.01f, -20f);
                    gl.Rotate(-90f, 1f, 0f, 0f);
                    gl.Color(1f, 1f, 1f);
                    Cylinder stick = new Cylinder();
                    stick.Slices = 20;
                    stick.BaseRadius = 0.3f;
                    stick.TopRadius = 0.3f;
                    stick.Height = 40f;
                    stick.CreateInContext(gl);
                    stick.TextureCoords = true;
                    stick.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
                    gl.PopMatrix();
                    gl.Enable(OpenGL.GL_CULL_FACE);
                    #endregion

                    #region Flag
                    gl.PushMatrix();
                    gl.Translate(20.2f, 38f, -20.2f);
                    gl.Rotate(-90f, 0f, 0f, 1f);
                    gl.Color(0.79f, 0.11f, 0.11f);
                    gl.Begin(OpenGL.GL_TRIANGLE_FAN);
                    gl.Vertex(2.5f, 10f, 0.2f);
                    gl.Vertex(0f, 0f, 0.4f);
                    gl.Vertex(5f, 0f, 0.4f);
                    gl.Vertex(5f, 0f, 0f);
                    gl.Vertex(0f, 0f, 0f);
                    gl.Vertex(0f, 0f, 0.4f);
                    gl.End();
                    gl.PopMatrix();
                    #endregion

                    #region Golf Tee
                    gl.PushMatrix();
                    gl.Translate(-20f, -1.8f, 20f);
                    gl.Rotate(-90f, 1f, 0f, 0f);
                    gl.Color(0.6f, 0.52f, 0.39f);
                    Cylinder tee = new Cylinder();
                    tee.Slices = 20;
                    tee.BaseRadius = 0f;
                    tee.TopRadius = 0.3f;
                    tee.Height = 3f;
                    tee.CreateInContext(gl);
                    tee.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
                    gl.PopMatrix();
                    #endregion

                    #region Golf Ball
                    gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.GolfBall]);
                    gl.PushMatrix();
                    gl.Translate(-20f + ballPosition_x_z, 2f + ballPosition_y, 20f - ballPosition_x_z);
                    gl.Color(1f, 1f, 1f);
                    Sphere ball = new Sphere();
                    ball.CreateInContext(gl);
                    ball.TextureCoords = true;
                    ball.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
                    gl.PopMatrix();
                    #endregion

                gl.PopMatrix();
                #endregion

                #region Golf Club
                gl.PushMatrix();
                gl.Translate(-20f, 4.5f, 18f);
                gl.Rotate(110f, 0f, 1f, 0f);
                gl.Rotate(GolfClubAngle, 1f, 0f, 0f);
                m_golf_club.Draw();
                gl.PopMatrix();
                #endregion
           
            gl.PopMatrix();

            #region Light
            gl.PushMatrix();
            gl.Translate(0f, 30f, -80f);
            gl.Color(1f, 1f, 1f);
            Sphere light = new Sphere();
            light.CreateInContext(gl);
            light.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
            gl.PopMatrix();
            #endregion

            #region Text
            gl.FrontFace(OpenGL.GL_CW);
            gl.Disable(OpenGL.GL_DEPTH_TEST);
            gl.PushMatrix();
            gl.Color(1f, 0f, 0f);
            gl.Translate(-37f, 19.5f, -50);
            gl.DrawText3D("Tahoma", 10f, 1f, 0.03f, "Predmet: Racunarska grafika");

            gl.Translate(-10.6f, -1.3f, 0f);
            gl.DrawText3D("Tahoma", 10f, 1f, 0.03f, "Sk. god: 2021/22.");

            gl.Translate(-6.6f, -1.3f, 0f);
            gl.DrawText3D("Tahoma", 10f, 1f, 0.03f, "Ime: Ana");

            gl.Translate(-3.5f, -1.3f, 0f);
            gl.DrawText3D("Tahoma", 10f, 1f, 0.03f, "Prezime: Gavrilovic");

            gl.Translate(-6.9f, -1.3f, 0f);
            gl.DrawText3D("Tahoma", 10f, 1f, 0.03f, "Sifra zad: 15.2");
            gl.PopMatrix();
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.FrontFace(OpenGL.GL_CCW);
            #endregion

            // Oznaci kraj iscrtavanja
            gl.Flush();
        }

        public void StartAnimation()
        {
            AnimationActive = true;
            timer1.Start();
        }

        public void RunGolfClubAnimation(object sender, EventArgs e)
        {
            if (golfClubAngle == 35f) golfClubGoingUp = false;
            if (golfClubAngle == 0f) golfClubGoingUp = true;

            if (golfClubAngle == -1f)
            {
                timer1.Stop();
                timer2.Start();
                golfClubGoingUp = true;
            }

            if (golfClubGoingUp) golfClubAngle += 1f;
            if (!golfClubGoingUp) golfClubAngle -= 2f;
        }

        public void RunBallAnimation(object sender, EventArgs e)
        {
            if (ballPosition_y <= -7f)
            {
                timer2.Stop();
                AnimationActive = false;

                ballPosition_x_z = -0.45f;
                ballPosition_y = 0f;
            }

            if (ballPosition_x_z >= 39f)
            {
                ballPosition_y -= 0.2f;
            } else
            {
                ballPosition_x_z += 0.5f;
                ballPosition_y -= 0.01f;
            }
        }

        // Podesava viewport i projekciju za OpenGL kontrolu.
        public void Resize(OpenGL gl, int width, int height)
        {
            m_width = width;
            m_height = height;

            // Definisanje projekcije i Viewporta
            gl.Viewport(0, 0, width, height);
            gl.MatrixMode(OpenGL.GL_PROJECTION);                            // selektuj Projection Matrix
            gl.LoadIdentity();
            gl.Perspective(45f, (double)width / height, 0.5f, 1000f);

            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();                                              // resetuj ModelView Matrix
        }

        // Implementacija IDisposable interfejsa.
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_golf_club.Dispose();
            }
        }

        #endregion Metode

        #region IDisposable metode

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable metode
    }
}
