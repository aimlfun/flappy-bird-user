using System.Drawing.Drawing2D;

namespace FlappyBird;

/// <summary>
///  __        __    _ _   ____                            
///  \ \      / /_ _| | | / ___|  ___  _ __ ___  ___  _ __ 
///   \ \ /\ / / _` | | | \___ \ / _ \ '_ \/ __|/ _ \| '__|
///    \ V V  / (_| | | |  ___) |  __/ | | \__ \ (_) | |   
///     \_/\_/ \__,_|_|_| |____/ \___|_| |_|___/\___/|_|   
/// </summary>
internal class WallSensor
{    
    /// <summary>
    /// Stores the "lines" that contain pipe.
    /// </summary>
    private readonly List<PointF[]> wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predatorLocation"></param>
    /// <param name="heatSensorRegionsOutput"></param>
    /// <returns></returns>
    internal double[] Read(List<Rectangle> pipesOnScreen,  PointF predatorLocation, out double[] heatSensorRegionsOutput)
    {
        wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates.Clear();
        
        // how many sample points it radiates to detect a pipe
        int SamplePoints = 32;

        heatSensorRegionsOutput = new double[SamplePoints];

        // e.g 
        // input to the neural network
        //   _ \ | / _   
        //   0 1 2 3 4 
        //        
        double fieldOfVisionStartInDegrees = -80;

        //   _ \ | / _   
        //   0 1 2 3 4
        //   [-] this
        double sensorVisionAngleInDegrees = 5;

        //   _ \ | / _   
        //   0 1 2 3 4
        //   ^ this
        double sensorAngleToCheckInDegrees = fieldOfVisionStartInDegrees;

        // how far it looks for a pipe
        double DepthOfVisionInPixels = 250;

        for (int LIDARangleIndex = 0; LIDARangleIndex < SamplePoints; LIDARangleIndex++)
        {
            //     -45  0  45
            //  -90 _ \ | / _ 90   <-- relative to direction of car, hence + angle car is pointing
            double LIDARangleToCheckInRadiansMin = MathUtils.DegreesInRadians(sensorAngleToCheckInDegrees);

            PointF p1 = new((float)(Math.Cos(LIDARangleToCheckInRadiansMin) * DepthOfVisionInPixels + predatorLocation.X),
                            (float)(Math.Sin(LIDARangleToCheckInRadiansMin) * DepthOfVisionInPixels + predatorLocation.Y));

            heatSensorRegionsOutput[LIDARangleIndex] = 0; // no target in this direction

            PointF intersection = new();

            // check each "target" rectangle and see if it intersects with the sensor.            
            foreach (Rectangle boundingBoxAroundPipe in pipesOnScreen)
            {
                // rectangle is bounding box for the pipe
                Point topRight = new(boundingBoxAroundPipe.Right, boundingBoxAroundPipe.Top);
                Point topLeft = new(boundingBoxAroundPipe.Left, boundingBoxAroundPipe.Top);
                Point bottomLeft = new(boundingBoxAroundPipe.Left, boundingBoxAroundPipe.Bottom);
                Point bottomRight = new(boundingBoxAroundPipe.Right, boundingBoxAroundPipe.Bottom);
      
                // check left side of pipe
                if (MathUtils.GetLineIntersection(predatorLocation, p1, topLeft, bottomLeft, out PointF intersectiona))
                {
                    double mult = 1 - MathUtils.DistanceBetweenTwoPoints(predatorLocation, intersectiona).Clamp(0F, (float)DepthOfVisionInPixels) / DepthOfVisionInPixels;

                    if (mult > heatSensorRegionsOutput[LIDARangleIndex])
                    {
                        heatSensorRegionsOutput[LIDARangleIndex] = mult;  // closest
                        intersection = intersectiona;
                    }
                }

                // check the bottom side of pipe (pipes coming out of ceiling)
                if (MathUtils.GetLineIntersection(predatorLocation, p1, bottomLeft, bottomRight, out PointF intersectionb))
                {
                    double mult = 1 - MathUtils.DistanceBetweenTwoPoints(predatorLocation, intersectionb).Clamp(0F, (float)DepthOfVisionInPixels) / DepthOfVisionInPixels;

                    if (mult > heatSensorRegionsOutput[LIDARangleIndex])
                    {
                        heatSensorRegionsOutput[LIDARangleIndex] = mult;  // closest
                        intersection = intersectionb;
                    }
                }

                // check the top of pipe (pipes coming out floor)
                if (MathUtils.GetLineIntersection(predatorLocation, p1, topLeft, topRight, out PointF intersectionc))
                {
                    double mult = 1 - MathUtils.DistanceBetweenTwoPoints(predatorLocation, intersectionc).Clamp(0F, (float)DepthOfVisionInPixels) / DepthOfVisionInPixels;

                    if (mult > heatSensorRegionsOutput[LIDARangleIndex])
                    {
                        heatSensorRegionsOutput[LIDARangleIndex] = mult;  // closest
                        intersection = intersectionc;
                    }
                }

                // we don't check right side, as we're looking forwards.
            }

            // detect where the floor is
            if (MathUtils.GetLineIntersection(predatorLocation, p1, new(0, 300), new(800, 300), out PointF intersection2))
            {
                double mult = 1 - MathUtils.DistanceBetweenTwoPoints(predatorLocation, intersection2).Clamp(0F, (float)DepthOfVisionInPixels) / DepthOfVisionInPixels;

                if (mult > heatSensorRegionsOutput[LIDARangleIndex])
                {
                    heatSensorRegionsOutput[LIDARangleIndex] = mult;  // closest
                    intersection = intersection2;
                }
            }

            if (heatSensorRegionsOutput[LIDARangleIndex] != 0)
            {
                wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates.Add(new PointF[] { predatorLocation, intersection });
            }

            //   _ \ | / _         _ \ | / _   
            //   0 1 2 3 4         0 1 2 3 4
            //  [-] from this       [-] to this
            sensorAngleToCheckInDegrees += sensorVisionAngleInDegrees;
        }

        return heatSensorRegionsOutput;
    }

    /// <summary>
    /// Draws lines radiating, show where pipe was detected.
    /// </summary>
    /// <param name="graphics"></param>
    internal void DrawWhereTargetIsInRespectToSweepOfHeatSensor(Graphics graphics)
    {
        using Pen pen = new(Color.FromArgb(60, 100, 100, 100));
        pen.DashStyle = DashStyle.Dot;
        
        // draw the heat sensor
        foreach (PointF[] point in wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates)
        {
            graphics.DrawLines(pen, point);
        }
    }
}