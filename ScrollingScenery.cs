using System.Security.Cryptography;

namespace FlappyBird;

// Images courtesy of: https://www.kindpng.com/downpng/iimbxmh_atlas-png-flappy-bird-transparent-png/
// "kindpng_1842511.png" was split into the parts. The chevron edges had to be adjusted, as do the
// Flappy bird.

internal class ScrollingScenery
{
    /// <summary>
    /// Parallax scrolling: background city moves slower. So we track where we are on 
    /// the background here.
    /// </summary>
    internal static int s_posBackground = 0;

    /// <summary>
    /// Parallax scrolling: chevron + pipes move independent of the background. This tracks where
    /// we are in the game (and thus where we draw the pipes).
    /// </summary>
    internal static int s_pos = 0;

    /// <summary>
    /// Width of the playable game area.
    /// </summary>
    private static int s_widthOfGameArea;

    /// <summary>
    /// Height of the playable game area.
    /// </summary>
    private static int s_heightOfGameArea;

    /// <summary>
    /// This is super inefficient. Image from above URL. More efficient would probably be to have a thin
    /// Chevron bitmap, and fill a rectangle the ground colour.
    /// </summary>
    private static Bitmap s_bitmapChevronFloor;

    /// <summary>
    /// This is super inefficient. Image from above URL. More efficient would probably be to have a layer 
    /// for buildings, and fill a rectangle for the sky.
    /// </summary>
    private static Bitmap s_bitmapSlowMovingBuildings;

    /// <summary>
    /// This is a pipe facing up (fat end at the top).
    /// </summary>
    private static Bitmap s_upPipe;

    /// <summary>
    /// This is a pipe facing down (fat end at the bottom).
    /// </summary>
    private static Bitmap s_downPipe;

    /// <summary>
    /// We make a list of pipe locations (left edge, and center).
    /// </summary>
    private static List<Point> s_pointsWhereRandomPipesAppearInTheScenery;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    internal ScrollingScenery(int width, int height)
    {
        // store dimensions
        s_widthOfGameArea = width;
        s_heightOfGameArea = height;

        if (s_upPipe is null)
        {
            InitialiseImages(width);

            CreateRandomPipes();
        }
    }

    /// <summary>
    /// Loads the images, and applies transparency.
    /// </summary>
    /// <param name="width"></param>
    private static void InitialiseImages(int width)
    {
        s_upPipe = new(@"images\pipeUP.png");
        s_upPipe.MakeTransparent();

        s_downPipe = new(@"images\pipeDOWN.png");
        s_downPipe.MakeTransparent();

        CreateScrollingChevronArea(width);

        CreateScrollingBuildingsWithSky(width);
    }

    /// <summary>
    /// Creates a large enough scrolling bitmap we can blit as we scroll.
    /// Part of the parallax, this is the floor the pipes scroll with.
    /// </summary>
    /// <param name="width"></param>
    /// <returns></returns>
    private static Graphics CreateScrollingChevronArea(int width)
    {
        // make the "scrolling" brown area into twice the size of the screen
        Bitmap bitmapBottom = new(@"images\chevronGround.png");

        s_bitmapChevronFloor = new(width * 2, bitmapBottom.Height);
        Graphics graphics = Graphics.FromImage(s_bitmapChevronFloor);

        for (int x = 0; x <= s_bitmapChevronFloor.Width; x += bitmapBottom.Width)
        {
            graphics.DrawImageUnscaled(bitmapBottom, x, 0);
        }

        return graphics;
    }

    /// <summary>
    /// Creates a large enough scrolling bitmap that we can blit as we slowly scroll.
    /// It contains sky (you cannot tell its scrolling) and the buildings.
    /// </summary>
    /// <param name="width"></param>
    private static void CreateScrollingBuildingsWithSky(int width)
    {
        // make the "scrolling" buildings area into twice the size of the screen.
        Bitmap bitmapBackground = new(@"images\buildingsAndSky.png");

        s_bitmapSlowMovingBuildings = new Bitmap(width * 2, bitmapBackground.Height);
        Graphics graphics = Graphics.FromImage(s_bitmapSlowMovingBuildings);

        for (int x = 0; x <= s_bitmapSlowMovingBuildings.Width; x += bitmapBackground.Width)
        {
            graphics.DrawImageUnscaled(bitmapBackground, x, 0);
        }
    }

    /// <summary>
    /// Creates the pipes.
    /// </summary>
    private static void CreateRandomPipes()
    {
        s_pointsWhereRandomPipesAppearInTheScenery = new();

        int horizontalDistanceBetweenPipes = 110;

        for (int i = 300; i < 20000; i += RandomNumberGenerator.GetInt32(94, 124) + horizontalDistanceBetweenPipes)
        {
            if (horizontalDistanceBetweenPipes > 0) horizontalDistanceBetweenPipes -= 3; // reduces the distance between pipes to make the level harder as it progresses

            s_pointsWhereRandomPipesAppearInTheScenery.Add(new Point(i, RandomNumberGenerator.GetInt32(30, s_bitmapSlowMovingBuildings.Height - 140)));
        }
    }

    /// <summary>
    /// Draws the parallax scrolling scenery including pipes, but not Flappy.
    /// </summary>
    /// <param name="graphics"></param>
    internal static void Draw(Graphics graphics)
    {
        // parallax scrolling, these two actually move at different rates

        // the top is SLOWER scrolling
        graphics.DrawImageUnscaled(s_bitmapSlowMovingBuildings, -(s_posBackground % 225), 0);

        DrawVisiblePipes(graphics);

        // the bottom is FASTER scrolling.
        graphics.DrawImageUnscaled(s_bitmapChevronFloor, -(s_pos % 264), s_heightOfGameArea - s_bitmapChevronFloor.Height);
    }

    /// <summary>
    /// Moves the scenery to the next point (progress game)
    /// </summary>
    internal static void Move()
    {
        s_pos++;

        // Parallax scrolling achieved by moving background every 4.
        if (s_pos % 4 == 0) ++s_posBackground;
    }

    /// <summary>
    /// Draws all the visible pipes. 
    /// </summary>
    /// <param name="graphics"></param>
    private static void DrawVisiblePipes(Graphics graphics)
    {
        int leftEdgeOfScreenTakingIntoAccountScrolling = s_pos;
        int rightEdgeOfScreenTakingIntoAccountScrolling = s_pos + s_widthOfGameArea;

        foreach (Point p in s_pointsWhereRandomPipesAppearInTheScenery)
        {
            if (p.X > rightEdgeOfScreenTakingIntoAccountScrolling) break; // offscreen to the right, no need to check for more

            if (p.X < leftEdgeOfScreenTakingIntoAccountScrolling - s_upPipe.Width) continue; // offscreen to the left

            // draw the top pipe
            graphics.DrawImageUnscaled(s_downPipe, p.X - leftEdgeOfScreenTakingIntoAccountScrolling, p.Y - s_downPipe.Height - 40);
            
            // draw the bottom pipe
            graphics.DrawImageUnscaled(s_upPipe, p.X - leftEdgeOfScreenTakingIntoAccountScrolling, p.Y + 40);
        }
    }

    /// <summary>
    /// Collision detection. Has flappy hit anything?
    /// </summary>
    /// <param name="flappy"></param>
    /// <returns>true - flappy collided with pipe | false - flappy has not hit anything.</returns>
    internal static bool FlappyCollidedWithPipe(Flappy flappy)
    {
        int left = s_pos;
        int right = Flappy.WidthOfAFlappyBirdPX + left;

        int score = 0;

        bool collided = false;

        foreach (Point p in s_pointsWhereRandomPipesAppearInTheScenery)
        {
            if (p.X < left)
            {
                ++score;
                continue;
            }

            if (p.X > right) break;

            // bird is before this pipe
            if (flappy.HorizontalPositionOfFlappyPX + left + Flappy.WidthOfAFlappyBirdPX < p.X) continue;

            ++score;

            Rectangle rectangleAreaBetweenVerticalPipes = new(p.X, p.Y - 30, 39, 60);

            if (!rectangleAreaBetweenVerticalPipes.Contains(p.X + 2, (int)flappy.VerticalPositionOfFlappyPX))
            {
                collided = true;
                break;
            }
        }

        flappy.Score = score; // provide a score

        return collided;
    }

    /// <summary>
    /// Get a list of pipes within a certain distance of Flappy.
    /// </summary>
    /// <param name="flappy"></param>
    /// <returns></returns>
    internal static List<Rectangle> GetClosestPipes(Flappy flappy, int distance)
    {
        int left = (int)(s_pos + flappy.HorizontalPositionOfFlappyPX)-40;
        int right = (int)(Flappy.WidthOfAFlappyBirdPX + left + flappy.HorizontalPositionOfFlappyPX)+ distance;

        int found = 0;
        List<Rectangle> result = new();

        foreach (Point p in s_pointsWhereRandomPipesAppearInTheScenery)
        {
            if (p.X < left) continue;
           
            ++found;

            if (p.X > right || found > 3) break; // limit, seeing 50 ahead doesn't help

            // add top pipe rectangle
            result.Add(new(p.X-s_pos, 0, 40, p.Y - 40));

            // add bottom pipe rectangle
            result.Add(new(p.X-s_pos, p.Y + 40, 40, s_heightOfGameArea - s_bitmapChevronFloor.Height - p.Y - 40));
        }

        return result;
    }
}