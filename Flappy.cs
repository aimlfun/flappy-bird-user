using System.Diagnostics;

namespace FlappyBird;

/// <summary>
/// Represents a "Flappy" bird.
/// </summary>
internal class Flappy
{
    /// <summary>
    /// true - it shows lines indicating sensor of pipes, and draws a box around the pipe.
    /// </summary>
    const bool c_testAndDrawSensor = false;

    /// <summary>
    /// Where Flappy is in the sky (0=top of screen).
    /// </summary>
    internal float VerticalPositionOfFlappyPX;

    /// <summary>
    /// Where flappy is horizontally (offset from left of screen).
    /// </summary>
    internal float HorizontalPositionOfFlappyPX;

    /// <summary>
    /// Width of a "Flappy" bird.
    /// </summary>
    internal static int WidthOfAFlappyBirdPX = 0;

    /// <summary>
    /// Height of a "Flappy" bird.
    /// </summary>
    internal static int HeightOfAFlappyBirdPX = 0;

    /// <summary>
    /// Track of score
    /// </summary>
    internal int Score = 0;

    /// <summary>
    /// true = hit pipe -> game-over (we don't do lives for AI training).
    /// false = in game.
    /// </summary>
    private bool FlappyWentSplat;

    /// <summary>
    /// How fast Flappy is moving in a vertical direction.
    /// </summary>
    private float verticalSpeed;

    /// <summary>
    /// How fast Flappy is accelerating up or down (or not at all).
    /// </summary>
    private float verticalAcceleration;

    /// <summary>
    /// 1=steady flight, 0..1..2=simulated flappping.
    /// </summary>
    private int wingFlappingAnimationFrame = 1;

    /// <summary>
    /// The 3 frames simulating the bird / flapping.
    /// </summary>
    private readonly static Bitmap[] s_FlappyImageFrames;

    /// <summary>
    /// Provides a "sensor", used by AI to detect pipes.
    /// </summary>
    WallSensor sensor = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    internal Flappy()
    {
        HorizontalPositionOfFlappyPX = 10;
        VerticalPositionOfFlappyPX = 100;
        FlappyWentSplat = false;
    }

    /// <summary>
    /// Cache images that apply to all instances of Flappy.
    /// </summary>
    static Flappy()
    {
        List<Bitmap> images = new()
        {
            // load images for Flappy to save us having to paint pixels.
            new(@"images\FlappyFrame1.png"),
            new(@"images\FlappyFrame2.png"),
            new(@"images\FlappyFrame3.png")
        };

        s_FlappyImageFrames = images.ToArray();

        // make the 3 images transparent (green part around the outside of Flappy)
        s_FlappyImageFrames[0].MakeTransparent();
        s_FlappyImageFrames[1].MakeTransparent();
        s_FlappyImageFrames[2].MakeTransparent();
        
        // width & height of a Flappy
        WidthOfAFlappyBirdPX = s_FlappyImageFrames[0].Width;
        HeightOfAFlappyBirdPX = s_FlappyImageFrames[0].Height;
    }

    /// <summary>
    /// Draws an animated "Flappy".
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        // draw Flappy (bird)
        graphics.DrawImageUnscaled(s_FlappyImageFrames[wingFlappingAnimationFrame], (int) HorizontalPositionOfFlappyPX, (int) (VerticalPositionOfFlappyPX - 10));
       
        // if flapping, toggle frames to sumulate, else keep wings still
        if (verticalAcceleration > 0) wingFlappingAnimationFrame = (wingFlappingAnimationFrame + 1) % s_FlappyImageFrames.Length; else wingFlappingAnimationFrame = 1;

#if c_testAndDrawSensor
        List<Rectangle> r = ScrollingScenery.GetClosestPipes(this, 300);

        sensor.Read(r, new PointF(HorizontalPositionOfFlappyPX+WidthOfAFlappyBirdPX-3, VerticalPositionOfFlappyPX-4), out double[] heatSensorRegionsOutput);

        foreach (Rectangle rect in r) graphics.DrawRectangle(Pens.AliceBlue, rect);

        sensor.DrawWhereTargetIsInRespectToSweepOfHeatSensor(graphics);
#endif
    }

    /// <summary>
    /// User pressed [Enter] to go up. Flappy is given an upwards acceleration and will animate flapping.
    /// </summary>
    internal void StartFlapping()
    {
        if (FlappyWentSplat) return; // if splat, then no upwards control, make him crash

        verticalAcceleration = 0.08f;
    }

    /// <summary>
    /// User released [Enter]. Flappy stops flapping, so we arrest upwards acceleration. Flappy will fall.
    /// </summary>
    internal void StopFlapping()
    {
        if (FlappyWentSplat) return;
     
        verticalAcceleration = 0;
    }

    /// <summary>
    /// Move Flappy.
    /// </summary>
    internal void Move()
    {
        // Flappy does not have an anti-gravity module. So we apply gravity acceleration (-). It's not "9.81" because we
        // don't know how much Flappy weighs nor wing thrust ratio.
        verticalAcceleration -= 0.001f;

        // Prevent Flappy falling too fast. Too early for terminal velocity, but makes the game "playable">
        if (verticalAcceleration < -1) verticalAcceleration = -1;
        
        // Apply acceleration to the speed.
        verticalSpeed -= verticalAcceleration;

        // Ensure Flappy doesn't go too quick (makes it unplayable).
        verticalSpeed = verticalSpeed.Clamp(-2, 3);

        VerticalPositionOfFlappyPX += verticalSpeed;

        // Stop Flappy going off screen (top/bottom).
        VerticalPositionOfFlappyPX = VerticalPositionOfFlappyPX.Clamp(0, 285);

        if (FlappyWentSplat) return; // no collision detection required.

        // if bird collided with the pipe, we set a flag (prevents control), and ensure it falls out the sky.
        if (ScrollingScenery.FlappyCollidedWithPipe(this))
        {
            verticalAcceleration = 0;
            verticalSpeed = 0;
            FlappyWentSplat = true;
        }
    }
}
