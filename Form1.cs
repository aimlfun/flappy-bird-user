namespace FlappyBird
{
    public partial class Form1 : Form
    {
        ScrollingScenery flappyScrollingScenery;
        Flappy flappy;

        public Form1()
        {
            InitializeComponent();

            flappy = new Flappy();
            flappyScrollingScenery = new(pictureBox1.Width, pictureBox1.Height);
            timerScroll.Start();
        }

        private void timerScroll_Tick(object sender, EventArgs e)
        {
            ScrollingScenery.Move();
            flappy.Move();
            
            Bitmap b = new(pictureBox1.Width, pictureBox1.Height);
            
            using Graphics g = Graphics.FromImage(b);

            ScrollingScenery.Draw(g);
            flappy.Draw(g);
            

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = b;
            Text = flappy.Score.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // [Enter] boosts flappy upwards
            if (e.KeyCode == Keys.Enter) flappy.StartFlapping();

            // [P] pauses game
            if (e.KeyCode == Keys.P) timerScroll.Enabled = !timerScroll.Enabled;
        }

        /// <summary>
        /// We need to stop the "up" moving when the [Enter] key is released.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) flappy.StopFlapping();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}